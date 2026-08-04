using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using GesAchats.Data.Context;

namespace GesAchats.Data.Context;

public class GesAchatsDbContextFactory : IDesignTimeDbContextFactory<GesAchatsDbContext>
{
    public GesAchatsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GesAchatsDbContext>();
        optionsBuilder.UseNpgsql("Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pwnqodqomtnfhbeiuzrf;Password=?ys7qd*?3GpW+?*;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;CommandTimeout=30;");

        return new GesAchatsDbContext(optionsBuilder.Options);
    }
}
