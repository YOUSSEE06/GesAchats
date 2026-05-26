using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using GesAchats.Data.Context;

namespace GesAchats.Data.Context;

public class GesAchatsDbContextFactory : IDesignTimeDbContextFactory<GesAchatsDbContext>
{
    public GesAchatsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GesAchatsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=GesAchatsDb;Username=postgres;Password=0000");

        return new GesAchatsDbContext(optionsBuilder.Options);
    }
}
