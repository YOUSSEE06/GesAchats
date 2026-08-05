using System.Net.Http.Headers;
using System.Net.Http.Json;
using Serilog;
using GesAchats.Core.Helpers;

namespace GesAchats.Core.Services;

/// <summary>
/// Client de l'API Supabase Storage (REST). Utilise la clé service_role du .env
/// (SUPABASE_SECRET_KEY) pour l'écriture (contourne les RLS) et l'anonyme pour l'URL
/// publique des téléchargements. Les buckets sont créés « public » si absents.
/// </summary>
public class SupabaseStorageClient
{
    /// <summary>Bucket dédié aux preuves de paiement importées.</summary>
    public const string ProofBucket = "preuves";

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly string _baseUrl;
    private readonly string _anonKey;
    private readonly string _serviceRole;

    public SupabaseStorageClient()
    {
        _baseUrl = (EnvLoader.Get("SUPABASE_URL") ?? string.Empty).TrimEnd('/');
        _anonKey = EnvLoader.Get("SUPABASE_ANON_KEY") ?? string.Empty;
        _serviceRole = EnvLoader.Get("SUPABASE_SECRET_KEY") ?? string.Empty;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrWhiteSpace(_serviceRole);

    private string Key => !string.IsNullOrWhiteSpace(_serviceRole) ? _serviceRole : _anonKey;

    private HttpRequestMessage CreateStorageRequest(HttpMethod method, string url, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", Key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Key);
        if (content != null)
        {
            request.Content = content;
        }
        return request;
    }

    /// <summary>Crée le bucket s'il n'existe pas (idempotent, bucket public pour les téléchargements).</summary>
    public async Task EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Supabase Storage non configuré : SUPABASE_URL / SUPABASE_SECRET_KEY manquants dans le .env.");
        }

        // Endpoint officiel : POST /storage/v1/bucket (singulier) pour la création.
        // ATTENTION : /storage/v1/buckets (pluriel) n'existe pas et répond 404.
        using var request = CreateStorageRequest(HttpMethod.Post, $"{_baseUrl}/storage/v1/bucket",
            JsonContent.Create(new { name = bucket, @public = true }));
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode) return;

        var text = await response.Content.ReadAsStringAsync(ct);
        // 400/409 / "duplicated" = le bucket existe déjà
        if ((int)response.StatusCode is 400 or 409 || text.Contains("duplicat", StringComparison.OrdinalIgnoreCase))
            return;

        Log.Error("Échec création bucket Supabase {Bucket}: {Status} {Body}", bucket, (int)response.StatusCode, text);
        throw new InvalidOperationException($"Impossible de créer le bucket Supabase « {bucket} » : {text}");
    }

    /// <summary>Envoie un fichier local dans le bucket (objectPath = dossier/nom syntax Windows-friendly).</summary>
    public async Task UploadObjectAsync(string bucket, string objectPath, string sourcePath, string contentType, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Supabase Storage non configuré : les preuves importées ne peuvent pas être sauvegardées en ligne.");
        }

        var bytes = await File.ReadAllBytesAsync(sourcePath, ct);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var safePath = SanitizeObjectPath(objectPath);
        using var request = CreateStorageRequest(HttpMethod.Put,
            $"{_baseUrl}/storage/v1/object/{bucket}/{safePath}", content);
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode) return;

        var text = await response.Content.ReadAsStringAsync(ct);
        Log.Error("Échec upload Supabase {Bucket}/{Path} : {Status} {Body}", bucket, safePath, (int)response.StatusCode, text);
        throw new InvalidOperationException(
            $"Impossible d'envoyer le fichier vers Supabase Storage ({bucket}/{safePath}).\nRéponse : {text}");
    }

    /// <summary>Récupère le contenu d'un objet via l'URL publique du bucket (aucune authentification requise).</summary>
    public async Task<byte[]?> DownloadObjectAsync(string bucket, string objectPath, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var safePath = SanitizeObjectPath(objectPath);
        using var response = await _http.GetAsync(
            $"{_baseUrl}/storage/v1/object/public/{bucket}/{safePath}", ct);

        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("Téléchargement Supabase {Bucket}/{Path} impossible : {Status}", bucket, safePath, (int)response.StatusCode);
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <summary>Remplace les caractères dangereux d'un nom de fichier tout en conservant les «/» de dossier.</summary>
    private static string SanitizeObjectPath(string objectPath)
    {
        var segments = objectPath.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = new string(segments[i]
                .Select(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-' ? c : '_')
                .ToArray());
        }
        return string.Join("/", segments).TrimStart('/');
    }
}