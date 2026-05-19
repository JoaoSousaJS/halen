using FluentAssertions;
using Halen.Application.AuditTrail.Queries;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Moq;

namespace Halen.UnitTests.AuditTrail;

[TestClass]
public class SearchAuditLogsQueryHandlerTests
{
    private static readonly Guid ClinicId = TestTenantContext.DefaultClinicId;
    private static readonly Guid Actor1 = Guid.NewGuid();
    private static readonly Guid Actor2 = Guid.NewGuid();

    [TestMethod]
    public async Task Handle_ReturnsPaginatedResults_OrderedByCreatedAtDescending()
    {
        var db = TestDbFactory.Create();
        SeedLogs(db, 5);
        var handler = CreateHandler(db);
        var query = new SearchAuditLogsQuery(null, null, null, null, null, null, Page: 1, PageSize: 3);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Logs.Should().HaveCount(3);
        result.Logs[0].Timestamp.Should().BeOnOrAfter(result.Logs[1].Timestamp);
        result.Logs[1].Timestamp.Should().BeOnOrAfter(result.Logs[2].Timestamp);
    }

    [TestMethod]
    public async Task Handle_FilterByActorId_ReturnsOnlyMatchingLogs()
    {
        var db = TestDbFactory.Create();
        db.AuditLogs.Add(CreateLog(actorId: Actor1, action: "A"));
        db.AuditLogs.Add(CreateLog(actorId: Actor2, action: "B"));
        db.AuditLogs.Add(CreateLog(actorId: Actor1, action: "C"));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(Actor1, null, null, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Logs.Should().AllSatisfy(l => l.ActorId.Should().Be(Actor1));
    }

    [TestMethod]
    public async Task Handle_FilterByAction_ReturnsExactMatch()
    {
        var db = TestDbFactory.Create();
        db.AuditLogs.Add(CreateLog(action: "BookAppointment"));
        db.AuditLogs.Add(CreateLog(action: "CancelAppointment"));
        db.AuditLogs.Add(CreateLog(action: "BookAppointment"));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(null, "BookAppointment", null, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Logs.Should().AllSatisfy(l => l.Action.Should().Be("BookAppointment"));
    }

    [TestMethod]
    public async Task Handle_FilterByTargetId_ReturnsExactMatch()
    {
        var targetId = Guid.NewGuid().ToString();
        var db = TestDbFactory.Create();
        db.AuditLogs.Add(CreateLog(targetId: targetId));
        db.AuditLogs.Add(CreateLog(targetId: Guid.NewGuid().ToString()));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(null, null, targetId, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Logs[0].TargetId.Should().Be(targetId);
    }

    [TestMethod]
    public async Task Handle_FilterByDateRange_ReturnsLogsWithinRange()
    {
        var db = TestDbFactory.Create();
        var now = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        db.AuditLogs.Add(CreateLog(createdAt: now.AddDays(-5)));
        db.AuditLogs.Add(CreateLog(createdAt: now.AddDays(-2)));
        db.AuditLogs.Add(CreateLog(createdAt: now));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(null, null, null, now.AddDays(-3), now.AddDays(-1), null), CancellationToken.None);

        result.TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_CombinedFilters_AppliesAll()
    {
        var db = TestDbFactory.Create();
        db.AuditLogs.Add(CreateLog(actorId: Actor1, action: "BookAppointment"));
        db.AuditLogs.Add(CreateLog(actorId: Actor1, action: "CancelAppointment"));
        db.AuditLogs.Add(CreateLog(actorId: Actor2, action: "BookAppointment"));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(Actor1, "BookAppointment", null, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Logs[0].ActorId.Should().Be(Actor1);
        result.Logs[0].Action.Should().Be("BookAppointment");
    }

    [TestMethod]
    public async Task Handle_NoMatchingLogs_ReturnsEmptyWithZeroCount()
    {
        var db = TestDbFactory.Create();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(null, null, null, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Logs.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_PlatformAdminWithClinicId_ReturnsOnlyThatClinic()
    {
        var otherClinic = Guid.NewGuid();
        var db = TestDbFactory.Create();
        db.AuditLogs.Add(CreateLog(action: "A"));
        db.AuditLogs.Add(CreateLog(action: "B", clinicId: otherClinic));
        db.AuditLogs.Add(CreateLog(action: "C"));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db, isPlatformAdmin: true);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(null, null, null, null, null, otherClinic), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Logs[0].Action.Should().Be("B");
    }

    [TestMethod]
    public async Task Handle_PlatformAdminWithoutClinicId_ReturnsAllClinics()
    {
        var otherClinic = Guid.NewGuid();
        var db = TestDbFactory.Create();
        db.AuditLogs.Add(CreateLog(action: "A"));
        db.AuditLogs.Add(CreateLog(action: "B", clinicId: otherClinic));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db, isPlatformAdmin: true);

        var result = await handler.Handle(
            new SearchAuditLogsQuery(null, null, null, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(2);
    }

    private static SearchAuditLogsQueryHandler CreateHandler(HalenDbContext db, bool isPlatformAdmin = false)
    {
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(t => t.IsPlatformAdmin).Returns(isPlatformAdmin);
        return new SearchAuditLogsQueryHandler(db, tenantContext.Object);
    }

    private static AuditLog CreateLog(
        Guid? actorId = null,
        string action = "TestAction",
        string? targetId = null,
        DateTime? createdAt = null,
        Guid? clinicId = null)
    {
        return new AuditLog
        {
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ClinicId = clinicId ?? ClinicId,
            ActorId = actorId ?? Actor1,
            ActorName = "Test User",
            Action = action,
            TargetId = targetId ?? Guid.NewGuid().ToString(),
            IpAddress = "127.0.0.1",
            Metadata = null
        };
    }

    private static void SeedLogs(HalenDbContext db, int count)
    {
        for (var i = 0; i < count; i++)
            db.AuditLogs.Add(CreateLog());
        db.SaveChanges();
    }
}
