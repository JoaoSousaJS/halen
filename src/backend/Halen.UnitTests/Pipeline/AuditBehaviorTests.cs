using FluentAssertions;
using Halen.Application.Interfaces;
using Halen.Application.Pipeline;
using Halen.Domain.Entities;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Pipeline;

[TestClass]
public class AuditBehaviorTests
{
    private static readonly Guid ClinicId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ActorId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    private Mock<ITenantContext> _tenantContextMock = null!;
    private Mock<IAuditContextProvider> _auditContextMock = null!;
    private Mock<ILogger<AuditBehavior<TestAuditableCommand, TestResult>>> _loggerMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(t => t.ClinicId).Returns(ClinicId);

        _auditContextMock = new Mock<IAuditContextProvider>();
        _auditContextMock.Setup(a => a.ActorId).Returns(ActorId);
        _auditContextMock.Setup(a => a.ActorName).Returns("Test User");
        _auditContextMock.Setup(a => a.IpAddress).Returns("127.0.0.1");

        _loggerMock = new Mock<ILogger<AuditBehavior<TestAuditableCommand, TestResult>>>();
    }

    [TestMethod]
    public async Task Handle_AuditableCommand_CreatesAuditLogAfterHandler()
    {
        var db = TestDbFactory.Create();
        var behavior = CreateBehavior(db);
        var command = new TestAuditableCommand(ActorId, "target-123");
        var expectedResult = new TestResult(true, Guid.NewGuid());

        var result = await behavior.Handle(command, _ => Task.FromResult(expectedResult), CancellationToken.None);

        result.Should().Be(expectedResult);
        var auditLog = db.AuditLogs.SingleOrDefault();
        auditLog.Should().NotBeNull();
        auditLog!.ClinicId.Should().Be(ClinicId);
        auditLog.ActorId.Should().Be(ActorId);
        auditLog.ActorName.Should().Be("Test User");
        auditLog.Action.Should().Be("TestAuditable");
        auditLog.TargetId.Should().Be("target-123");
        auditLog.IpAddress.Should().Be("127.0.0.1");
    }

    [TestMethod]
    public async Task Handle_NonAuditableCommand_SkipsAudit()
    {
        var db = TestDbFactory.Create();
        var nonAuditBehavior = new AuditBehavior<NonAuditableCommand, TestResult>(
            db, _tenantContextMock.Object, _auditContextMock.Object,
            new Mock<ILogger<AuditBehavior<NonAuditableCommand, TestResult>>>().Object);
        var command = new NonAuditableCommand("test");
        var expectedResult = new TestResult(true, Guid.NewGuid());

        var result = await nonAuditBehavior.Handle(command, _ => Task.FromResult(expectedResult), CancellationToken.None);

        result.Should().Be(expectedResult);
        db.AuditLogs.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_QueryRequest_SkipsAudit()
    {
        var db = TestDbFactory.Create();
        var queryBehavior = new AuditBehavior<TestQuery, TestResult>(
            db, _tenantContextMock.Object, _auditContextMock.Object,
            new Mock<ILogger<AuditBehavior<TestQuery, TestResult>>>().Object);
        var query = new TestQuery();
        var expectedResult = new TestResult(true, Guid.NewGuid());

        var result = await queryBehavior.Handle(query, _ => Task.FromResult(expectedResult), CancellationToken.None);

        result.Should().Be(expectedResult);
        db.AuditLogs.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_HandlerThrows_NoAuditLogCreated()
    {
        var db = TestDbFactory.Create();
        var behavior = CreateBehavior(db);
        var command = new TestAuditableCommand(ActorId, "target-123");

        var act = () => behavior.Handle(command, _ => throw new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        db.AuditLogs.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_HandlerReturnsErrorResult_AuditLogStillCreated()
    {
        var db = TestDbFactory.Create();
        var behavior = CreateBehavior(db);
        var command = new TestAuditableCommand(ActorId, "target-123");
        var errorResult = new TestResult(false, null);

        var result = await behavior.Handle(command, _ => Task.FromResult(errorResult), CancellationToken.None);

        result.Should().Be(errorResult);
        db.AuditLogs.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Handle_AuditSaveFails_ReturnsOriginalResponse()
    {
        var dbMock = new Mock<IAppDbContext>();
        dbMock.Setup(d => d.AuditLogs).Returns(TestDbFactory.Create().AuditLogs);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB write failed"));

        var behavior = new AuditBehavior<TestAuditableCommand, TestResult>(
            dbMock.Object, _tenantContextMock.Object, _auditContextMock.Object, _loggerMock.Object);
        var command = new TestAuditableCommand(ActorId, "target-123");
        var expectedResult = new TestResult(true, Guid.NewGuid());

        var result = await behavior.Handle(command, _ => Task.FromResult(expectedResult), CancellationToken.None);

        result.Should().Be(expectedResult);
    }

    [TestMethod]
    public async Task Handle_IpAddress_ExtractedFromAuditContextProvider()
    {
        _auditContextMock.Setup(a => a.IpAddress).Returns("10.0.0.42");
        var db = TestDbFactory.Create();
        var behavior = CreateBehavior(db);
        var command = new TestAuditableCommand(ActorId, null);

        await behavior.Handle(command, _ => Task.FromResult(new TestResult(true, Guid.NewGuid())), CancellationToken.None);

        db.AuditLogs.Single().IpAddress.Should().Be("10.0.0.42");
    }

    [TestMethod]
    public async Task Handle_ActorName_ExtractedFromAuditContextProvider()
    {
        _auditContextMock.Setup(a => a.ActorName).Returns("Jane Admin");
        var db = TestDbFactory.Create();
        var behavior = CreateBehavior(db);
        var command = new TestAuditableCommand(ActorId, null);

        await behavior.Handle(command, _ => Task.FromResult(new TestResult(true, Guid.NewGuid())), CancellationToken.None);

        db.AuditLogs.Single().ActorName.Should().Be("Jane Admin");
    }

    [TestMethod]
    public async Task Handle_MetadataSerialized_ContainsCommandProperties()
    {
        var db = TestDbFactory.Create();
        var behavior = CreateBehavior(db);
        var command = new TestAuditableCommand(ActorId, "tgt-1");

        await behavior.Handle(command, _ => Task.FromResult(new TestResult(true, Guid.NewGuid())), CancellationToken.None);

        var metadata = db.AuditLogs.Single().Metadata;
        metadata.Should().NotBeNullOrEmpty();
        metadata.Should().Contain("tgt-1");
    }

    [TestMethod]
    public async Task Handle_RedactedProperty_MetadataContainsRedactedMarker()
    {
        var db = TestDbFactory.Create();
        var redactBehavior = new AuditBehavior<RedactedCommand, TestResult>(
            db, _tenantContextMock.Object, _auditContextMock.Object,
            new Mock<ILogger<AuditBehavior<RedactedCommand, TestResult>>>().Object);
        var command = new RedactedCommand(ActorId, "secret-data", "visible-data");

        await redactBehavior.Handle(command, _ => Task.FromResult(new TestResult(true, Guid.NewGuid())), CancellationToken.None);

        var metadata = db.AuditLogs.Single().Metadata!;
        metadata.Should().Contain("[REDACTED]");
        metadata.Should().NotContain("secret-data");
        metadata.Should().Contain("visible-data");
    }

    [TestMethod]
    public async Task Handle_CommandWithNoActorId_FallsBackToAuditContext()
    {
        var db = TestDbFactory.Create();
        var fallbackBehavior = new AuditBehavior<NoActorCommand, TestResult>(
            db, _tenantContextMock.Object, _auditContextMock.Object,
            new Mock<ILogger<AuditBehavior<NoActorCommand, TestResult>>>().Object);
        var command = new NoActorCommand("some-action");

        await fallbackBehavior.Handle(command, _ => Task.FromResult(new TestResult(true, Guid.NewGuid())), CancellationToken.None);

        db.AuditLogs.Single().ActorId.Should().Be(ActorId);
    }

    [TestMethod]
    public async Task Handle_TargetIdNull_ExtractsFromResultId()
    {
        var db = TestDbFactory.Create();
        var behavior = CreateBehavior(db);
        var resultId = Guid.NewGuid();
        var command = new TestAuditableCommand(ActorId, null);

        await behavior.Handle(command, _ => Task.FromResult(new TestResult(true, resultId)), CancellationToken.None);

        db.AuditLogs.Single().TargetId.Should().Be(resultId.ToString());
    }

    private AuditBehavior<TestAuditableCommand, TestResult> CreateBehavior(
        HalenDbContext db)
    {
        return new AuditBehavior<TestAuditableCommand, TestResult>(
            db, _tenantContextMock.Object, _auditContextMock.Object, _loggerMock.Object);
    }

    public record TestAuditableCommand(Guid ActorId, string? TargetId) : IRequest<TestResult>, IAuditableCommand
    {
        Guid IAuditableCommand.ActorId => ActorId;
        string? IAuditableCommand.AuditTargetId => TargetId;
    }

    public record NonAuditableCommand(string Name) : IRequest<TestResult>;

    public record TestQuery() : IRequest<TestResult>;

    public record TestResult(bool Success, Guid? Id);

    public record RedactedCommand(Guid ActorId, [property: AuditRedact] string Secret, string Visible)
        : IRequest<TestResult>, IAuditableCommand
    {
        Guid IAuditableCommand.ActorId => ActorId;
    }

    public record NoActorCommand(string Data) : IRequest<TestResult>, IAuditableCommand;
}
