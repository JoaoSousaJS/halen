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
public class GetRecordAccessMatrixQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetRecordAccessMatrixQueryHandler _handler = null!;
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
        var grantedByUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _adminUserId, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin },
            new User { Id = patientUserId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", UserName = "jane@test.com", Role = UserRole.Patient },
            new User { Id = doctorUserId, FirstName = "Dr", LastName = "Smith", Email = "dr@test.com", UserName = "dr@test.com", Role = UserRole.Doctor },
            new User { Id = grantedByUserId, FirstName = "Clinic", LastName = "Admin", Email = "clinic@test.com", UserName = "clinic@test.com", Role = UserRole.ClinicAdmin }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });

        var accessId = Guid.NewGuid();
        _db.RecordAccesses.Add(new RecordAccess
        {
            Id = accessId,
            PatientProfileId = _patientProfileId,
            GrantedToUserId = doctorUserId,
            AccessLevel = RecordAccessLevel.Full,
            GrantedAt = DateTime.UtcNow.AddDays(-30),
            GrantedByUserId = grantedByUserId,
            RevokedAt = null
        });

        _db.RecordAccessLogs.Add(new RecordAccessLog
        {
            Id = Guid.NewGuid(),
            PatientProfileId = _patientProfileId,
            AccessedByUserId = doctorUserId,
            AccessedAt = DateTime.UtcNow.AddDays(-1),
            Action = "View",
            ResourceType = "PatientRecord",
            IpAddress = "127.0.0.1"
        });

        await _db.SaveChangesAsync();

        _handler = new GetRecordAccessMatrixQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_Success_ReturnsAccessMatrix()
    {
        var query = new GetRecordAccessMatrixQuery(_adminUserId, UserRole.PlatformAdmin, _patientProfileId, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Entries.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);

        var entry = result.Entries[0];
        entry.UserName.Should().Be("Dr Smith");
        entry.UserRole.Should().Be("Doctor");
        entry.AccessLevel.Should().Be("Full");
        entry.GrantedBy.Should().Be("Clinic Admin");
        entry.RevokedAt.Should().BeNull();
        entry.LastViewed.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Handle_NotAdmin_ReturnsForbidden()
    {
        var nonAdminId = Guid.NewGuid();
        var query = new GetRecordAccessMatrixQuery(nonAdminId, UserRole.Doctor, _patientProfileId, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_Pagination_RespectsPageSize()
    {
        // Add more access entries
        var userId2 = Guid.NewGuid();
        var grantedById = Guid.NewGuid();
        _db.Users.AddRange(
            new User { Id = userId2, FirstName = "Nurse", LastName = "Joy", Email = "nurse@test.com", UserName = "nurse@test.com", Role = UserRole.Doctor },
            new User { Id = grantedById, FirstName = "Sys", LastName = "Admin", Email = "sys@test.com", UserName = "sys@test.com", Role = UserRole.PlatformAdmin }
        );
        _db.RecordAccesses.Add(new RecordAccess
        {
            PatientProfileId = _patientProfileId,
            GrantedToUserId = userId2,
            AccessLevel = RecordAccessLevel.Limited,
            GrantedAt = DateTime.UtcNow.AddDays(-10),
            GrantedByUserId = grantedById
        });
        await _db.SaveChangesAsync();

        var query = new GetRecordAccessMatrixQuery(_adminUserId, UserRole.PlatformAdmin, _patientProfileId, 1, 1);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Entries.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
    }
}
