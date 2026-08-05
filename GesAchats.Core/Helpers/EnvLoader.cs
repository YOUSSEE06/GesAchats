using Npgsql;

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

    /// <summary>Alias vers SUPABASE_PUBLISHABLE_KEY (nouvelle convention Supabase).</summary>
    public static string? SupabasePublishableKey => Get("SUPABASE_PUBLISHABLE_KEY") ?? Get("SUPABASE_ANON_KEY");

    /// <summary>Email du compte administrateur (bootstrap uniquement).</summary>
    public static string? AdminEmail => Get("ADMIN_EMAIL");

    /// <summary>Mot de passe du compte administrateur (bootstrap uniquement).</summary>
    public static string? AdminPassword => Get("ADMIN_PASSWORD");

    /// <summary>
    /// Liste ordonnée des endpoints pooler Supavisor (IPv4) à tester par défaut
    /// si SUPABASE_HOST échoue. Format : (host, port, label).
    /// On teste eu-west-2 (Londres) en premier (région réelle du projet), puis eu-west-1 (Irlande) en fallback.
    /// </summary>
    public static (string Host, int Port, string Label)[] FallbackPoolerEndpoints { get; } = new[]
    {
        ("aws-0-eu-west-2.pooler.supabase.com", 5432, "Pooler aws-0 eu-west-2 session (5432)"),
        ("aws-1-eu-west-2.pooler.supabase.com", 5432, "Pooler aws-1 eu-west-2 session (5432)"),
        ("aws-0-eu-west-2.pooler.supabase.com", 6543, "Pooler aws-0 eu-west-2 transaction (6543)"),
        ("aws-1-eu-west-2.pooler.supabase.com", 6543, "Pooler aws-1 eu-west-2 transaction (6543)"),
        ("aws-0-eu-west-1.pooler.supabase.com", 5432, "Pooler aws-0 eu-west-1 session (5432)"),
        ("aws-1-eu-west-1.pooler.supabase.com", 5432, "Pooler aws-1 eu-west-1 session (5432)"),
    };

    /// <summary>
    /// Construit la chaîne de connexion Npgsql à partir des variables SUPABASE_* du .env.
    /// Utilise NpgsqlConnectionStringBuilder pour un échappement correct des valeurs
    /// (mots de passe avec caractères spéciaux, etc.).
    /// </summary>
    public static string BuildConnectionString()
    {
        return BuildConnectionString(
            host: Get("SUPABASE_HOST"),
            port: Get("SUPABASE_PORT"),
            database: Get("SUPABASE_DATABASE"),
            username: Get("SUPABASE_USERNAME"),
            password: Get("SUPABASE_PASSWORD"),
            sslMode: Get("SUPABASE_SSL_MODE"),
            trustCert: Get("SUPABASE_TRUST_CERTIFICATE"));
    }

    /// <summary>
    /// Construit une chaîne de connexion Npgsql avec remplacement à la volée de certains
    /// paramètres (utilisé pour tenter plusieurs endpoints pooler).
    /// </summary>
    public static string BuildConnectionString(
        string? host,
        string? port = null,
        string? database = null,
        string? username = null,
        string? password = null,
        string? sslMode = null,
        string? trustCert = null)
    {
        var finalHost = host ?? Get("SUPABASE_HOST") ?? string.Empty;
        var finalPortRaw = port ?? Get("SUPABASE_PORT") ?? "5432";
        var finalDatabase = database ?? Get("SUPABASE_DATABASE") ?? "postgres";
        var finalUsername = username ?? Get("SUPABASE_USERNAME") ?? string.Empty;
        var finalPassword = password ?? Get("SUPABASE_PASSWORD") ?? string.Empty;
        var finalSslMode = sslMode ?? Get("SUPABASE_SSL_MODE") ?? "Require";
        var finalTrustCert = trustCert ?? Get("SUPABASE_TRUST_CERTIFICATE") ?? "true";

        if (string.IsNullOrWhiteSpace(finalHost) || string.IsNullOrWhiteSpace(finalUsername) || string.IsNullOrWhiteSpace(finalPassword))
        {
            throw new InvalidOperationException(
                "Variables manquantes dans le fichier .env (SUPABASE_HOST / SUPABASE_USERNAME / SUPABASE_PASSWORD).");
        }

        if (!int.TryParse(finalPortRaw, out var finalPort))
        {
            finalPort = 5432;
        }

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = finalHost,
            Port = finalPort,
            Database = finalDatabase,
            Username = finalUsername,
            Password = finalPassword,
            SslMode = Enum.TryParse<SslMode>(finalSslMode, ignoreCase: true, out var ssl) ? ssl : SslMode.Require,
            Timeout = 15,
            CommandTimeout = 30,
            Pooling = true,
            Multiplexing = false,
        };

        if (bool.TryParse(finalTrustCert, out var tc) && tc)
        {
            csb.TrustServerCertificate = true;
        }

        return csb.ConnectionString;
    }

    /// <summary>
    /// Extrait le Project Ref Supabase depuis SUPABASE_URL ou SUPABASE_USERNAME.
    /// Exemple : "ukntkgwtkspyjzrnlmxg" pour "https://ukntkgwtkspyjzrnlmxg.supabase.co".
    /// </summary>
    public static string? InferProjectRef()
    {
        var url = Get("SUPABASE_URL");
        if (!string.IsNullOrWhiteSpace(url))
        {
            var m = System.Text.RegularExpressions.Regex.Match(url, @"https?://([a-z0-9]{20})\.supabase\.co",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
        }

        var user = Get("SUPABASE_USERNAME");
        if (!string.IsNullOrWhiteSpace(user) && user.StartsWith("postgres.", StringComparison.OrdinalIgnoreCase))
        {
            var rest = user.Substring("postgres.".Length);
            if (rest.Length == 20) return rest;
        }

        return null;
    }

    /// <summary>
    /// Construit le nom d'utilisateur pooler standard ("postgres.{ref}") à partir
    /// du Project Ref inféré ou fourni.
    /// </summary>
    public static string BuildPoolerUsername(string? projectRef = null)
    {
        var pr = projectRef ?? InferProjectRef();
        if (string.IsNullOrWhiteSpace(pr))
        {
            return Get("SUPABASE_USERNAME") ?? string.Empty;
        }
        return $"postgres.{pr}";
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

        var hashIndex = value.IndexOf(" #", StringComparison.Ordinal);
        if (hashIndex >= 0)
        {
            value = value[..hashIndex].TrimEnd();
        }

        return value;
    }
}