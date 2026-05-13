using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Halen.Infrastructure.Persistence;

// Used only by EF Core design-time tools (dotnet ef migrations add ...).
// Avoids running the full app host (which would require Jwt:Secret to be set).
public class HalenDbContextFactory : IDesignTimeDbContextFactory<HalenDbContext>
{
    public HalenDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Halen.API"))
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseNpgsql(config.GetConnectionString("Default"))
            .Options;

        return new HalenDbContext(options);
    }
}
