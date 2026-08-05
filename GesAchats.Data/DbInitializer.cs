using GesAchats.Core.Entities;
using GesAchats.Core.Helpers;
using GesAchats.Core.Services;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GesAchats.Data;

/// <summary>
/// Seeding minimal : rôles obligatoires + compte administrateur lié à Supabase Auth.
/// Aucune donnée métier n'est insérée (base de données vide délibérée).
/// </summary>
public static class DbInitializer
{
    private static readonly (string Code, string Label, string Description)[] DefaultRoles =
    {
        ("ADMIN", "Administrateur", "Accès complet au système"),
        ("ACHETEUR", "Acheteur", "Gestion des besoins, devis, bons de commande et fournisseurs"),
        ("COMPTABLE", "Comptable", "Gestion des factures et des paiements"),
        ("MAGASINIER", "Magasinier", "Gestion du stock, des besoins et des livraisons")
    };

    public static async Task SeedRolesAsync(GesAchatsDbContext context)
    {
        foreach (var (code, label, description) in DefaultRoles)
        {
            if (!await context.Roles.AnyAsync(r => r.Code == code))
            {
                context.Roles.Add(new Role { Code = code, Label = label, Description = description });
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Garantit l'existence d'un compte administrateur dans public.Users et crée/relie son compte
    /// Supabase Auth (mot de passe : ADMIN_PASSWORD du fichier .env). L'email ADMIN_EMAIL n'est
    /// utilisé que s'il n'existe aucun administrateur en base.
    /// </summary>
    public static async Task BootstrapAdminAsync(GesAchatsDbContext context, SupabaseAuthClient authClient)
    {
        var adminRoleId = await context.Roles.Where(r => r.Code == "ADMIN").Select(r => r.Id).FirstOrDefaultAsync();
        if (adminRoleId == 0)
        {
            Log.Warning("Rôle ADMIN absent : bootstrap du compte admin ignoré.");
            return;
        }

        var admin = await context.Users.FirstOrDefaultAsync(u => u.RoleId == adminRoleId);
        if (admin == null)
        {
            var email = EnvLoader.AdminEmail?.Trim().ToLowerInvariant();
            var password = EnvLoader.AdminPassword;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Log.Warning("ADMIN_EMAIL / ADMIN_PASSWORD absents du .env : compte admin non créé.");
                return;
            }

            admin = new User
            {
                Login = email,
                FullName = "Administrateur",
                Email = email,
                PasswordHash = string.Empty, // le mot de passe est géré par Supabase Auth.
                RoleId = adminRoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            Log.Information("Compte administrateur créé pour {Email}", email);
        }

        if (admin.SupabaseAuthId != null)
        {
            return;
        }

        var adminPassword = EnvLoader.AdminPassword;
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            Log.Warning("ADMIN_PASSWORD absent du .env : compte admin non lié à Supabase.");
            return;
        }

        var signUp = await authClient.SignUpAsync(admin.Email, adminPassword);
        if (signUp.success && signUp.userId.HasValue)
        {
            admin.SupabaseAuthId = signUp.userId;
            admin.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            Log.Information("Compte admin Supabase créé pour {Email}", admin.Email);
            return;
        }

        if (signUp.alreadyExists)
        {
            var signIn = await authClient.SignInWithPasswordAsync(admin.Email, adminPassword);
            if (signIn.success && signIn.userId.HasValue)
            {
                admin.SupabaseAuthId = signIn.userId;
                admin.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                Log.Information("Compte admin Supabase lié pour {Email}", admin.Email);
                return;
            }

            Log.Warning("Email admin {Email} déjà enregistré chez Supabase avec un autre mot de passe : utilisez « Mot de passe oublié » ou créez-le dans le Dashboard Supabase.", admin.Email);
            return;
        }

        Log.Warning("Liaison Supabase du compte admin impossible : {Message}", signUp.message);
    }
}