using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GesAchats.Core.Helpers;
using Serilog;

namespace GesAchats.Core.Services;

/// <summary>
/// Client de l'API Auth Supabase (GoTrue). Utilise uniquement l'anon key (publique)
/// définie dans le fichier .env (SUPABASE_URL / SUPABASE_ANON_KEY). Aucune clé secrète n'est embarquée.
/// </summary>
public class SupabaseAuthClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _anonKey;

    public SupabaseAuthClient()
    {
        _baseUrl = (EnvLoader.Get("SUPABASE_URL") ?? string.Empty).TrimEnd('/');
        _anonKey = EnvLoader.Get("SUPABASE_ANON_KEY") ?? string.Empty;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrWhiteSpace(_anonKey);

    private (bool success, string message) ValidateConfig()
    {
        if (!IsConfigured)
        {
            return (false, "Configuration Supabase Auth incomplète : SUPABASE_URL / SUPABASE_ANON_KEY manquants dans le fichier .env.");
        }
        return (true, string.Empty);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl, object? body)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{relativeUrl}");
        request.Headers.Add("apikey", _anonKey);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }

    /// <summary>
    /// Connexion email + mot de passe. Retourne l'identifiant Supabase (UUID) et les jetons d'accès.
    /// </summary>
    public async Task<(bool success, Guid? userId, string? accessToken, string? refreshToken, string message)>
        SignInWithPasswordAsync(string email, string password)
    {
        var config = ValidateConfig();
        if (!config.success) return (false, null, null, null, config.message);

        try
        {
            using var response = await _http.SendAsync(CreateRequest(HttpMethod.Post,
                "/auth/v1/token?grant_type=password",
                new { email, password }));

            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var token = JsonSerializer.Deserialize<TokenResponse>(json);
                if (Guid.TryParse(token?.User?.Id, out var id))
                {
                    return (true, id, token!.AccessToken, token.RefreshToken, "OK");
                }
                return (false, null, null, null, "Réponse du service d'authentification invalide.");
            }

            var err = JsonSerializer.Deserialize<ErrorResponse>(json);
            Log.Warning("Supabase sign-in failed for {Email}: {Error}", email, err?.ErrorDescription ?? err?.Message ?? err?.Error ?? json);
            return (false, null, null, null, "Identifiants incorrects.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Supabase sign-in error for {Email}", email);
            return (false, null, null, null, "Impossible de contacter le service d'authentification.");
        }
    }

    /// <summary>
    /// Crée un compte Supabase. alreadyExists = true si l'email est déjà enregistré (compte à lier à la première connexion).
    /// </summary>
    public async Task<(bool success, bool alreadyExists, Guid? userId, string message)>
        SignUpAsync(string email, string password)
    {
        var config = ValidateConfig();
        if (!config.success) return (false, false, null, config.message);

        try
        {
            using var response = await _http.SendAsync(CreateRequest(HttpMethod.Post,
                "/auth/v1/signup",
                new { email, password }));

            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var signup = JsonSerializer.Deserialize<SignupResponse>(json);
                Guid.TryParse(signup?.Id, out var id);
                return (true, false, id == Guid.Empty ? null : id, "Compte créé.");
            }

            var err = JsonSerializer.Deserialize<ErrorResponse>(json);
            var raw = $"{err?.Error} {err?.Message} {err?.ErrorDescription}".Trim();
            if (raw.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            {
                return (false, true, null, "Email déjà enregistré.");
            }
            if (raw.Contains("signups_not_allowed", StringComparison.OrdinalIgnoreCase))
            {
                return (false, false, null, "L'inscription publique est désactivée dans Supabase (Authentication → Sign In / Up).");
            }

            Log.Warning("Supabase sign-up failed for {Email}: {Error}", email, raw);
            return (false, false, null, string.IsNullOrWhiteSpace(raw) ? "Inscription refusée par le service d'authentification." : raw);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Supabase sign-up error for {Email}", email);
            return (false, false, null, "Impossible de contacter le service d'authentification.");
        }
    }

    /// <summary>
    /// Envoie l'email de réinitialisation du mot de passe (auth/v1/recover).
    /// </summary>
    public async Task<(bool success, string message)> RecoverAsync(string email)
    {
        var config = ValidateConfig();
        if (!config.success) return (false, config.message);

        try
        {
            using var response = await _http.SendAsync(CreateRequest(HttpMethod.Post,
                "/auth/v1/recover",
                new { email }));

            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return (true, "Email de réinitialisation envoyé.");
            }

            var err = JsonSerializer.Deserialize<ErrorResponse>(json);
            Log.Warning("Supabase recover failed for {Email}: {Error}", email, err?.Error ?? err?.Message ?? json);
            return (false, "Impossible d'envoyer l'email de réinitialisation.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Supabase recover error for {Email}", email);
            return (false, "Impossible de contacter le service d'authentification.");
        }
    }

    /// <summary>
    /// Change le mot de passe d'un utilisateur connecté (nécessite son jeton d'accès).
    /// </summary>
    public async Task<(bool success, string message)> UpdatePasswordAsync(string accessToken, string newPassword)
    {
        var config = ValidateConfig();
        if (!config.success) return (false, config.message);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return (false, "Session expirée. Veuillez vous reconnecter.");
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Put, "/auth/v1/user", new { password = newPassword });
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return (true, "Mot de passe modifié.");
            }

            var err = JsonSerializer.Deserialize<ErrorResponse>(json);
            Log.Warning("Supabase update password failed: {Error}", err?.Error ?? err?.ErrorDescription ?? json);
            return (false, "Échec de la modification du mot de passe.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Supabase update password error");
            return (false, "Impossible de contacter le service d'authentification.");
        }
    }

    /// <summary>
    /// Modifie l'email du compte Supabase de l'utilisateur connecté (nécessite son jeton d'accès).
    /// </summary>
    public async Task<(bool success, string message)> UpdateEmailAsync(string accessToken, string newEmail)
    {
        var config = ValidateConfig();
        if (!config.success) return (false, config.message);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return (false, "Session expirée. Veuillez vous reconnecter.");
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Put, "/auth/v1/user", new { email = newEmail });
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return (true, "Email modifié.");
            }

            var err = JsonSerializer.Deserialize<ErrorResponse>(json);
            Log.Warning("Supabase update email failed for {Email}: {Error}", newEmail, err?.Error ?? err?.ErrorDescription ?? json);
            return (false, "Échec de la modification de l'email chez Supabase.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Supabase update email error");
            return (false, "Impossible de contacter le service d'authentification.");
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("user")]
        public AuthUserResponse? User { get; set; }
    }

    private sealed class AuthUserResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email_confirmed_at")]
        public string? EmailConfirmedAt { get; set; }
    }

    private sealed class SignupResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private sealed class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("msg")]
        public string? Message { get; set; }
    }
}