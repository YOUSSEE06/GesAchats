using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using GesAchats.Data.Context;
using GesAchats.Core.Helpers;

namespace GesAchats.Data.Context;

public class GesAchatsDbContextFactory : IDesignTimeDbContextFactory<GesAchatsDbContext>
{
    public GesAchatsDbContext CreateDbContext(string[] args)
    {
        // Design-time (migrations) : charge le .env depuis le dossier courant
        // (normalement le projet WPF) ou le dossier de la solution.
        EnvLoader.Load(Environment.CurrentDirectory);

        var optionsBuilder = new DbContextOptionsBuilder<GesAchatsDbContext>();
        optionsBuilder.UseNpgsql(EnvLoader.BuildConnectionString());

        return new GesAchatsDbContext(optionsBuilder.Options);
    }
}