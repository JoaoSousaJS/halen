using Halen.UnitTests.Helpers;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Application.MedicalRecords.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class GetRecordAccessLogsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetRecordAccessLogsQueryHandler _handler = null!;
    private Guid _adminUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _adminUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();

        var patientUserId = Guid.NewGuid();
        var doctorUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _adminUserId, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin },
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = doctorUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });

        _db.RecordAccessLogs.AddRange(
            new RecordAccessLog
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                AccessedByUserId = doctorUserId,
                AccessedAt = DateTime.UtcNow.AddHours(-2),
                Action = "View",
                ResourceType = "PatientRecord",
                IpAddress = "127.0.0.1"
            },
            new RecordAccessLog
            {
                Id = Guid.NewGuid(),
                PatientProfileId = _patientProfileId,
                AccessedByUserId = doctorUserId,
                AccessedAt = DateTime.UtcNow.AddHours(-1),
                Action = "Download",
                ResourceType = "MedicalDocument",
                IpAddress = "10.0.0.1"
            }
        );

        await _db.SaveChangesAsync();

        _handler = new GetRecordAccessLogsQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsAuditLogs()
    {
        var query = new GetRecordAccessLogsQuery(_adminUserId, UserRole.PlatformAdmin, _patientProfileId, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Logs.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        result.Logs.Should().AllSatisfy(log =>
        {
            log.AccessedBy.Should().Be("Dr Smith");
        });

        result.Logs[0].Action.Should().Be("Download"); // Most recent first
    }

    [TestMethod]
    public async Task Handle_NotAdmin_ReturnsForbidden()
    {
        var nonAdminId = Guid.NewGuid();
        var query = new GetRecordAccessLogsQuery(nonAdminId, UserRole.Doctor, _patientProfileId, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}
