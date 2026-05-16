using Halen.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Halen.Infrastructure.Persistence;

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

        return new HalenDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid ClinicId => Guid.Empty;
        public bool IsPlatformAdmin => true;
    }
}
