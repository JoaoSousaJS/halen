using System.Text;
using FluentAssertions;
using Halen.Application.AuditTrail.Queries;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Moq;

namespace Halen.UnitTests.AuditTrail;

[TestClass]
public class ExportAuditLogsCsvQueryHandlerTests
{
    private static readonly Guid ClinicId = TestTenantContext.DefaultClinicId;

    [TestMethod]
    public async Task Handle_GeneratesValidCsvWithHeadersAndRows()
    {
        var db = TestDbFactory.Create();
        db.AuditLogs.Add(CreateLog("BookAppointment", "user@test.com"));
        db.AuditLogs.Add(CreateLog("CancelAppointment", "admin@test.com"));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null);

        var result = await handler.Handle(query, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.CsvBytes);
        csv.Should().Contain("Timestamp,ActorName,Action,TargetId,IpAddress,Metadata");
        csv.Should().Contain("BookAppointment");
        csv.Should().Contain("CancelAppointment");
        result.FileName.Should().StartWith("audit-log-").And.EndWith(".csv");
    }

    [TestMethod]
    public async Task Handle_Respects10000RowCap()
    {
        var db = TestDbFactory.Create();
        for (var i = 0; i < 10_050; i++)
            db.AuditLogs.Add(CreateLog("Action" + i, "user"));
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null);

        var result = await handler.Handle(query, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.CsvBytes);
        var dataRows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1;
        dataRows.Should().BeLessOrEqualTo(10_000);
    }

    [TestMethod]
    public async Task Handle_CsvEscapesValuesWithCommasAndQuotes()
    {
        var db = TestDbFactory.Create();
        var log = CreateLog("Test", "User, \"Admin\"");
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null);

        var result = await handler.Handle(query, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.CsvBytes);
        csv.Should().Contain("\"User, \"\"Admin\"\"\"");
    }

    [TestMethod]
    public async Task Handle_CsvSanitizesFormulaInjectionCharacters()
    {
        var db = TestDbFactory.Create();
        var log = CreateLog("=SUM(A1)", "user");
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
        var handler = CreateHandler(db);
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null);

        var result = await handler.Handle(query, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.CsvBytes);
        csv.Should().NotContain(",=SUM");
    }

    [TestMethod]
    public async Task Handle_EmptyResult_ReturnsCsvWithHeaderOnly()
    {
        var db = TestDbFactory.Create();
        var handler = CreateHandler(db);
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), null);

        var result = await handler.Handle(query, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.CsvBytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1);
        lines[0].Should().Contain("Timestamp");
    }

    private static ExportAuditLogsCsvQueryHandler CreateHandler(HalenDbContext db)
    {
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(t => t.IsPlatformAdmin).Returns(false);
        return new ExportAuditLogsCsvQueryHandler(db, tenantContext.Object);
    }

    private static AuditLog CreateLog(string action = "TestAction", string actorName = "Test User")
    {
        return new AuditLog
        {
            ClinicId = ClinicId,
            ActorId = Guid.NewGuid(),
            ActorName = actorName,
            Action = action,
            TargetId = Guid.NewGuid().ToString(),
            IpAddress = "127.0.0.1",
            Metadata = null
        };
    }
}
