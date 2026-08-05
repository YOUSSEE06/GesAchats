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
    private readonly string _adminKey;

    public SupabaseAuthClient()
    {
        _baseUrl = (EnvLoader.Get("SUPABASE_URL") ?? string.Empty).TrimEnd('/');
        _anonKey = EnvLoader.Get("SUPABASE_ANON_KEY") ?? string.Empty;
        _adminKey = EnvLoader.Get("SUPABASE_SECRET_KEY") ?? string.Empty;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrWhiteSpace(_anonKey);

    /// <summary>Disponible si SUPABASE_SECRET_KEY (service_role) est renseignée dans le .env.</summary>
    public bool HasAdminKey => !string.IsNullOrWhiteSpace(_adminKey);

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
    /// Requête sur l'API Admin GoTrue avec la clé service_role (SUPABASE_SECRET_KEY).
    /// Utilisée UNIQUEMENT pour le bootstrap du compte administrateur.
    /// </summary>
    private HttpRequestMessage CreateAdminRequest(HttpMethod method, string relativeUrl, object? body)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{relativeUrl}");
        request.Headers.Add("apikey", _adminKey);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminKey);
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
            var raw = $"{err?.Error} {err?.ErrorDescription} {err?.Message}".Trim();
            Log.Warning("Supabase sign-in failed for {Email}: {Error}", email, raw);
            if (raw.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, null, null, "Email non confirmé : vérifiez votre boîte mail ou utilisez « Mot de passe oublié ».");
            }
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
    /// Bootstrap administrateur via l'API Admin (service_role) : crée le compte avec le mot de passe
    /// fourni et l'email confirmé, ou — s'il existe déjà — réinitialise son mot de passe et confirme
    /// son email. Contourne les restrictions « signups_not_allowed » et la confirmation d'email.
    /// Retourne success = true et l'identifiant Supabase lorsque le mot de passe est garanti comme étant celui fourni.
    /// </summary>
    public async Task<(bool success, Guid? userId, string message)>
        AdminBootstrapUserAsync(string email, string password)
    {
        if (!IsConfigured)
        {
            return (false, null, "Configuration Supabase Auth incomplète : SUPABASE_URL / SUPABASE_ANON_KEY manquants dans le fichier .env.");
        }
        if (!HasAdminKey)
        {
            return (false, null, "SUPABASE_SECRET_KEY absente du .env : bootstrap admin via service_role impossible.");
        }

        try
        {
            // 1) Création directe : mot de passe + email confirmé en une seule fois.
            using (var create = CreateAdminRequest(HttpMethod.Post, "/auth/v1/admin/users",
                       new { email, password, email_confirm = true }))
            using (var createResp = await _http.SendAsync(create))
            {
                var createJson = await createResp.Content.ReadAsStringAsync();
                if (createResp.IsSuccessStatusCode)
                {
                    var created = JsonSerializer.Deserialize<AdminUserResponse>(createJson);
                    Guid.TryParse(created?.Id, out var createdId);
                    return (true, createdId == Guid.Empty ? null : createdId, "Compte admin créé avec le mot de passe fourni.");
                }

                var createErr = JsonSerializer.Deserialize<ErrorResponse>(createJson);
                var createRaw = $"{createErr?.Error} {createErr?.ErrorDescription} {createErr?.Message}".Trim();
                if (!createRaw.Contains("already registered", StringComparison.OrdinalIgnoreCase)
                    && !createRaw.Contains("already_exists", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning("Supabase admin user creation failed for {Email}: {Error}", email, createRaw);
                    return (false, null, string.IsNullOrWhiteSpace(createRaw)
                        ? "Création du compte admin refusée."
                        : createRaw);
                }
            }

            // 2) Email déjà enregistré : recherche de l'identifiant puis réinitialisation du mot de passe.
            using (var list = CreateAdminRequest(HttpMethod.Get,
                       $"/auth/v1/admin/users?filter={Uri.EscapeDataString(email)}", null))
            using (var listResp = await _http.SendAsync(list))
            {
                var listJson = await listResp.Content.ReadAsStringAsync();
                if (!listResp.IsSuccessStatusCode)
                {
                    Log.Warning("Supabase admin user lookup failed for {Email}: {Error}", email, listJson);
                    return (false, null, "Recherche du compte admin impossible.");
                }

                var listDoc = JsonSerializer.Deserialize<AdminListResponse>(listJson);
                var target = listDoc?.Users?.FirstOrDefault(u =>
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                if (target == null || !Guid.TryParse(target.Id, out var targetId))
                {
                    return (false, null, "Compte admin introuvable chez Supabase.");
                }

                using var update = CreateAdminRequest(HttpMethod.Put,
                    $"/auth/v1/admin/users/{targetId}",
                    new { password, email_confirm = true });
                using var updateResp = await _http.SendAsync(update);
                var updateJson = await updateResp.Content.ReadAsStringAsync();
                if (updateResp.IsSuccessStatusCode)
                {
                    return (true, targetId, "Mot de passe admin réinitialisé et email confirmé.");
                }

                var updateErr = JsonSerializer.Deserialize<ErrorResponse>(updateJson);
                Log.Warning("Supabase admin password update failed for {Email}: {Error}", email,
                    updateErr?.Error ?? updateErr?.ErrorDescription ?? updateJson);
                return (false, null, "Échec de la réinitialisation du mot de passe admin.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Supabase admin bootstrap error for {Email}", email);
            return (false, null, "Impossible de contacter le service d'authentification.");
        }
    }

    /// <summary>
    /// Change le mot de passe d'un compte Supabase existant via l'API Admin (service_role)
    /// et confirme son email. Indépendant des limites d'envoi d'emails (auth/v1/recover).
    /// </summary>
    public async Task<(bool success, string message)> AdminResetPasswordAsync(string email, string newPassword)
    {
        if (!IsConfigured)
        {
            return (false, "Configuration Supabase Auth incomplète : SUPABASE_URL / SUPABASE_ANON_KEY manquants dans le fichier .env.");
        }
        if (!HasAdminKey)
        {
            return (false, "SUPABASE_SECRET_KEY absente du .env : impossible de modifier le mot de passe chez Supabase.");
        }

        try
        {
            using (var list = CreateAdminRequest(HttpMethod.Get,
                       $"/auth/v1/admin/users?filter={Uri.EscapeDataString(email)}", null))
            using (var listResp = await _http.SendAsync(list))
            {
                var listJson = await listResp.Content.ReadAsStringAsync();
                if (!listResp.IsSuccessStatusCode)
                {
                    Log.Warning("Supabase admin user lookup failed for {Email}: {Error}", email, listJson);
                    return (false, "Recherche du compte impossible chez Supabase.");
                }

                var listDoc = JsonSerializer.Deserialize<AdminListResponse>(listJson);
                var target = listDoc?.Users?.FirstOrDefault(u =>
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                if (target == null || !Guid.TryParse(target.Id, out var targetId))
                {
                    return (false, "Aucun compte actif trouvé pour cet email.");
                }

                using var update = CreateAdminRequest(HttpMethod.Put,
                    $"/auth/v1/admin/users/{targetId}",
                    new { password = newPassword, email_confirm = true });
                using var updateResp = await _http.SendAsync(update);
                var updateJson = await updateResp.Content.ReadAsStringAsync();
                if (updateResp.IsSuccessStatusCode)
                {
                    return (true, "Mot de passe modifié chez Supabase.");
                }

                var updateErr = JsonSerializer.Deserialize<ErrorResponse>(updateJson);
                Log.Warning("Supabase admin password update failed for {Email}: {Error}", email,
                    updateErr?.Error ?? updateErr?.ErrorDescription ?? updateJson);
                return (false, "Échec de la modification du mot de passe chez Supabase.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Supabase admin reset password error for {Email}", email);
            return (false, "Impossible de contacter le service d'authentification.");
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
            var raw = $"{err?.Error} {err?.ErrorDescription} {err?.Message}".Trim();
            Log.Warning("Supabase recover failed for {Email}: {Error}", email, raw);
            if (raw.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
                || err?.Code == 429)
            {
                return (false, "Trop de demandes envoyées récemment. Veuillez réessayer dans quelques minutes.");
            }
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

    private sealed class AdminListResponse
    {
        [JsonPropertyName("users")]
        public List<AdminUserResponse>? Users { get; set; }
    }

    private sealed class AdminUserResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    private sealed class ErrorResponse
    {
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("msg")]
        public string? Message { get; set; }
    }
}