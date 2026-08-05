using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

var projectRef = "ukntkgwtkspyjzrnlmxg";
var poolerUser = $"postgres.{projectRef}";
var password = Environment.GetEnvironmentVariable("SUPABASE_PASSWORD_TESTCONN")
               ?? "COLLEZ_ICI_VOTRE_MOT_DE_PASSE_DB";

var candidates = new (string Host, int Port, string Label)[]
{
    ("aws-0-eu-west-2.pooler.supabase.com", 5432, "Pooler aws-0 eu-west-2 session (5432) — PRIMARY DANS .ENV"),
    ("aws-1-eu-west-2.pooler.supabase.com", 5432, "Pooler aws-1 eu-west-2 session (5432)"),
    ("aws-0-eu-west-2.pooler.supabase.com", 6543, "Pooler aws-0 eu-west-2 transaction (6543)"),
    ("aws-1-eu-west-2.pooler.supabase.com", 6543, "Pooler aws-1 eu-west-2 transaction (6543)"),
    ("aws-0-eu-west-1.pooler.supabase.com", 5432, "Pooler aws-0 eu-west-1 session (5432) fallback"),
    ($"db.{projectRef}.supabase.co", 5432, $"Connexion directe db.{projectRef}.supabase.co (IPv6 only)"),
};

Console.WriteLine("=== PROJET : " + projectRef + "   Utilisateur pooler : " + poolerUser + " (Mot de passe lu depuis la variable d'environnement SUPABASE_PASSWORD_TESTCONN) ===\n");

foreach (var (host, port, label) in candidates)
{
    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Database = "postgres",
        Username = host.Contains("pooler.supabase.com") ? poolerUser : "postgres",
        Password = password,
        SslMode = SslMode.Require,
        Timeout = 12,
        CommandTimeout = 20,
    };
    TestConn(csb.ConnectionString, label);
}

static void TestConn(string connStr, string label)
{
    Console.WriteLine("▶ " + label);
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var conn = new NpgsqlConnection(connStr);
        conn.OpenAsync(cts.Token).GetAwaiter().GetResult();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT current_user, current_database(), inet_server_addr(), now()";
        using var rdr = cmd.ExecuteReader();
        if (rdr.Read())
        {
            Console.WriteLine("  ✅ SUCCÈS   user=" + rdr[0] + "  db=" + rdr[1] + "  server=" + rdr[2] + "  ts=" + rdr[3]);
        }
    }
    catch (Exception ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        var type = ex.GetType().Name;
        if (msg.Contains("tenant", StringComparison.OrdinalIgnoreCase) || msg.Contains("ENOTFOUND", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine("  ❌ tenant/user NOT FOUND — mdp pas encore propagé au pooler OU mdp faux. Attendre 90s + reset DB password dans le Dashboard si ça persiste.");
        else if (msg.Contains("aucune donnée du type requise", StringComparison.OrdinalIgnoreCase) || msg.Contains("no such host", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine("  ❌ DNS/Socket IPv6 — ce réseau n'a pas d'IPv6. Attendez-vous à ça sur la connexion directe et utilisez le pooler.");
        else if (msg.Contains("password authentication", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine("  ❌ Mot de passe Postgres invalide. Réinitialisez dans Settings → Database → Reset database password.");
        else
            Console.WriteLine("  ❌ " + type + " : " + Truncate(msg, 200));
    }
    Console.WriteLine();
}

static string Truncate(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n) + "…";
