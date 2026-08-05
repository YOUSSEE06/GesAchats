using Serilog;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class FileStorageService : IFileStorageService
{
    /// <summary>Préfixe des références stockées sur Supabase Storage.</summary>
    public const string RemotePrefix = "sb://";

    private readonly string _basePath;
    private readonly SupabaseStorageClient _storage;

    public FileStorageService(string basePath, SupabaseStorageClient storage)
    {
        _basePath = basePath;
        _storage = storage;

        // S'assurer que les dossiers de base existent
        Directory.CreateDirectory(Path.Combine(_basePath, "Documents", "Preuves"));
        Directory.CreateDirectory(Path.Combine(_basePath, "Documents", "Recus"));
    }

    /// <summary>
    /// Les preuves IMPORTÉES sont envoyées sur Supabase Storage, puis référencées
    /// par une référence distante (sb://bucket/...). En cas d'absence de configuration
    /// on retombe sur le stockage local pour ne jamais bloquer l'application.
    /// </summary>
    public async Task<string> SavePaymentProofAsync(int supplierId, int invoiceId, string sourcePath)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Le fichier source est introuvable", sourcePath);

        string extension = Path.GetExtension(sourcePath);
        string dateFolder = DateTime.Now.ToString("yyyyMM");

        if (_storage.IsConfigured)
        {
            string objectPath = $"{dateFolder}/{supplierId}/RECU_{invoiceId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";

            // Pas de repli silencieux : si Supabase est configuré et que l'envoi échoue,
            // l'erreur remonte (elle sera affichée) au lieu d'enregistrer localement sans prévenir.
            Log.Information("Upload preuve vers Supabase Storage : {Source} -> {Bucket}/{Path}",
                sourcePath, SupabaseStorageClient.ProofBucket, objectPath);
            await _storage.EnsureBucketAsync(SupabaseStorageClient.ProofBucket);
            Log.Information("Bucket {Bucket} prêt", SupabaseStorageClient.ProofBucket);
            await _storage.UploadObjectAsync(SupabaseStorageClient.ProofBucket, objectPath, sourcePath, ContentTypeFor(sourcePath));
            Log.Information("Preuve uploadée : {Bucket}/{Path}", SupabaseStorageClient.ProofBucket, objectPath);
            return $"{RemotePrefix}{SupabaseStorageClient.ProofBucket}/{objectPath}";
        }

        // Fallback local (ancien comportement) si Supabase non configuré ou en erreur
        string relativeDir = Path.Combine("Documents", "Preuves", dateFolder, supplierId.ToString());
        string fullDir = Path.Combine(_basePath, relativeDir);

        Directory.CreateDirectory(fullDir);

        string fileName = $"RECU_{invoiceId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        string fullPath = Path.Combine(fullDir, fileName);

        await Task.Run(() => File.Copy(sourcePath, fullPath, true));

        return Path.Combine(relativeDir, fileName);
    }

    /// <summary>Reçus générés par le logiciel : toujours en local, jamais envoyés sur Supabase.</summary>
    public async Task<string> SavePaymentReceiptAsync(int supplierId, int invoiceId, string sourcePath)
    {
        // Similaire à SavePaymentProof mais dans le dossier Recus
        string extension = Path.GetExtension(sourcePath);
        string dateFolder = DateTime.Now.ToString("yyyyMM");
        string relativeDir = Path.Combine("Documents", "Recus", dateFolder);
        string fullDir = Path.Combine(_basePath, relativeDir);

        Directory.CreateDirectory(fullDir);

        string fileName = $"RECU_GEN_{invoiceId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        string fullPath = Path.Combine(fullDir, fileName);

        await Task.Run(() => File.Copy(sourcePath, fullPath, true));

        return Path.Combine(relativeDir, fileName);
    }

    /// <summary>
    /// Fichier importé générique (pièce jointe de facture, etc.) : upload vers Supabase
    /// quand il est configuré, sinon repli local. Aucun repli silencieux si configuré.
    /// </summary>
    public async Task<string> UploadImportedFileAsync(string bucket, string objectPrefix, string sourcePath)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Le fichier source est introuvable", sourcePath);

        string dateFolder = DateTime.Now.ToString("yyyyMM");
        string extension = Path.GetExtension(sourcePath);

        if (_storage.IsConfigured)
        {
            string objectPath = $"{dateFolder}/{objectPrefix}/{DateTime.Now:yyyyMMddHHmmss}{extension}";
            Log.Information("Upload fichier importé vers Supabase Storage : {Source} -> {Bucket}/{Path}",
                sourcePath, bucket, objectPath);
            await _storage.EnsureBucketAsync(bucket);
            await _storage.UploadObjectAsync(bucket, objectPath, sourcePath, ContentTypeFor(sourcePath));
            Log.Information("Fichier importé uploadé : {Bucket}/{Path}", bucket, objectPath);
            return $"{RemotePrefix}{bucket}/{objectPath}";
        }

        string relativeDir = Path.Combine("Documents", "Imports", dateFolder, objectPrefix ?? "divers");
        string fullDir = Path.Combine(_basePath, relativeDir);
        Directory.CreateDirectory(fullDir);

        string fileName = $"{DateTime.Now:yyyyMMddHHmmss}{extension}";
        string fullPath = Path.Combine(fullDir, fileName);

        await Task.Run(() => File.Copy(sourcePath, fullPath, true));

        return Path.Combine(relativeDir, fileName);
    }

    /// <summary>Un fichier distant (sb://) est considéré comme « existant » : il est téléchargé au moment de l'ouverture.</summary>
    public bool FileExists(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return false;
        if (relativePath.StartsWith(RemotePrefix)) return true;
        return File.Exists(Path.Combine(_basePath, relativePath));
    }

    public string GetFullPath(string relativePath) =>
        GetFullPathAsync(relativePath).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Pour une référence distante, télécharge l'objet dans un cache local puis renvoie le chemin local.
    /// Pour une référence locale, renvoie simplement le chemin complet.
    /// </summary>
    public async Task<string> GetFullPathAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;

        if (relativePath.StartsWith(RemotePrefix))
        {
            var (bucket, objectPath) = SplitRemote(relativePath);
            string cachePath = Path.Combine(_basePath, "Documents", "SupabaseCache", bucket, objectPath);

            if (File.Exists(cachePath)) return cachePath;

            try
            {
                byte[]? bytes = await _storage.DownloadObjectAsync(bucket, objectPath);
                if (bytes is null) return cachePath; // fichier introuvable : l'appelant affichera l'erreur

                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                await File.WriteAllBytesAsync(cachePath, bytes);
                Log.Information("Preuve téléchargée depuis Supabase dans le cache local : {Path}", cachePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Échec téléchargement Supabase {Ref}", relativePath);
            }

            return cachePath;
        }

        return Path.Combine(_basePath, relativePath);
    }

    private static (string bucket, string objectPath) SplitRemote(string reference)
    {
        string rest = reference.Substring(RemotePrefix.Length).TrimStart('/');
        int idx = rest.IndexOf('/');
        if (idx <= 0) return (rest, string.Empty);
        return (rest.Substring(0, idx), rest.Substring(idx + 1));
    }

    private static string ContentTypeFor(string sourcePath)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".bmp"] = "image/bmp",
            [".gif"] = "image/gif",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".txt"] = "text/plain",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        };
        string ext = Path.GetExtension(sourcePath);
        return map.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }
}