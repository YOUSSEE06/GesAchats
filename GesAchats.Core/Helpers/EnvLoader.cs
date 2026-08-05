namespace GesAchats.Core.Helpers;

/// <summary>
/// Charge un fichier .env (situé à côté de l'exécutable) et expose les variables
/// sous forme de variables d'environnement. Aucun secret n'est codé en dur ici.
/// </summary>
public static class EnvLoader
{
    private const string EnvFileName = ".env";

    public static bool Exists(string basePath)
    {
        return File.Exists(Path.Combine(basePath, EnvFileName));
    }

    public static void Load(string basePath)
    {
        var envPath = Path.Combine(basePath, EnvFileName);
        if (!File.Exists(envPath))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = line[..eq].Trim();
            var value = ParseValue(line[(eq + 1)..].Trim());

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public static string? Get(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }

    /// <summary>URL du projet Supabase (Auth REST).</summary>
    public static string? SupabaseUrl => Get("SUPABASE_URL");

    /// <summary>Clé publique Supabase (anon key) utilisée pour Auth côté client.</summary>
    public static string? SupabaseAnonKey => Get("SUPABASE_ANON_KEY");

    /// <summary>Email du compte administrateur (bootstrap uniquement).</summary>
    public static string? AdminEmail => Get("ADMIN_EMAIL");

    /// <summary>Mot de passe du compte administrateur (bootstrap uniquement).</summary>
    public static string? AdminPassword => Get("ADMIN_PASSWORD");

    /// <summary>
    /// Construit la chaîne de connexion Npgsql à partir des variables SUPABASE_* du .env.
    /// </summary>
    public static string BuildConnectionString()
    {
        var host = Get("SUPABASE_HOST") ?? string.Empty;
        var port = Get("SUPABASE_PORT") ?? "5432";
        var database = Get("SUPABASE_DATABASE") ?? "postgres";
        var username = Get("SUPABASE_USERNAME") ?? string.Empty;
        var password = Get("SUPABASE_PASSWORD") ?? string.Empty;
        var sslMode = Get("SUPABASE_SSL_MODE") ?? "Require";
        var trustCert = Get("SUPABASE_TRUST_CERTIFICATE") ?? "true";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Variables manquantes dans le fichier .env (SUPABASE_HOST / SUPABASE_USERNAME / SUPABASE_PASSWORD).");
        }

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};" +
               $"SSL Mode={sslMode};Trust Server Certificate={trustCert};Timeout=15;CommandTimeout=30;";
    }

    private static string ParseValue(string value)
    {
        if (value.Length >= 2)
        {
            if (value[0] == '"' && value[^1] == '"')
            {
                return value[1..^1];
            }

            if (value[0] == '\'' && value[^1] == '\'')
            {
                return value[1..^1];
            }
        }

        // Retire un commentaire en fin de ligne (précédé d'un espace) si présent.
        var hashIndex = value.IndexOf(" #", StringComparison.Ordinal);
        if (hashIndex >= 0)
        {
            value = value[..hashIndex].TrimEnd();
        }

        return value;
    }
}