using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Halen.UnitTests.Helpers;

/// <summary>
/// Factory for creating in-memory HalenDbContext instances for unit tests.
/// Each call to Create() produces an isolated database with a unique name.
/// </summary>
public static class TestDbFactory
{
    /// <summary>
    /// Creates a new HalenDbContext backed by an in-memory database with
    /// transaction warnings suppressed and the default TestTenantContext.
    /// </summary>
    public static HalenDbContext Create() =>
        Create(new TestTenantContext());

    /// <summary>
    /// Creates a new HalenDbContext backed by an in-memory database with
    /// transaction warnings suppressed and a custom tenant context.
    /// </summary>
    public static HalenDbContext Create(TestTenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new ILikeReplacingInterceptor())
            .Options;

        return new HalenDbContext(options, tenantContext);
    }
}
