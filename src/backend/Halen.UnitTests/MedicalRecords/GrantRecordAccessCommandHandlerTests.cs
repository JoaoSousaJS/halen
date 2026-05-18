using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class GrantRecordAccessCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private GrantRecordAccessCommandHandler _handler = null!;
    private Guid _adminUserId;
    private Guid _doctorUserId;
    private Guid _patientProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _adminUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _adminUserId, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin },
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = patientUserId });
        await _db.SaveChangesAsync();

        _handler = new GrantRecordAccessCommandHandler(
            _db, new TestTenantContext(),
            Mock.Of<ILogger<GrantRecordAccessCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesAccess()
    {
        var command = new GrantRecordAccessCommand(
            _adminUserId, _patientProfileId, _doctorUserId,
            RecordAccessLevel.Full, "Doctor needs access for treatment");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessId.Should().NotBeNull();

        var access = await _db.RecordAccesses.FindAsync(result.AccessId);
        access.Should().NotBeNull();
        access!.PatientProfileId.Should().Be(_patientProfileId);
        access.GrantedToUserId.Should().Be(_doctorUserId);
        access.AccessLevel.Should().Be(RecordAccessLevel.Full);
        access.GrantedByUserId.Should().Be(_adminUserId);
        access.Reason.Should().Be("Doctor needs access for treatment");
        access.RevokedAt.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_NotAdmin_ReturnsForbidden()
    {
        var command = new GrantRecordAccessCommand(
            _doctorUserId, _patientProfileId, _doctorUserId,
            RecordAccessLevel.Full, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
        result.Error.Should().Contain("platform administrator");
    }

    [TestMethod]
    public async Task Handle_PatientNotFound_ReturnsNotFound()
    {
        var command = new GrantRecordAccessCommand(
            _adminUserId, Guid.NewGuid(), _doctorUserId,
            RecordAccessLevel.Full, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("Patient");
    }

    [TestMethod]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        var command = new GrantRecordAccessCommand(
            _adminUserId, _patientProfileId, Guid.NewGuid(),
            RecordAccessLevel.Full, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("User");
    }

    [TestMethod]
    public async Task Handle_ExistingAccess_UpdatesInsteadOfCreating()
    {
        // Seed an existing access record
        var existingAccessId = Guid.NewGuid();
        _db.RecordAccesses.Add(new RecordAccess
        {
            Id = existingAccessId,
            PatientProfileId = _patientProfileId,
            GrantedToUserId = _doctorUserId,
            AccessLevel = RecordAccessLevel.Limited,
            GrantedAt = DateTime.UtcNow.AddDays(-7),
            GrantedByUserId = _adminUserId,
        });
        await _db.SaveChangesAsync();

        var command = new GrantRecordAccessCommand(
            _adminUserId, _patientProfileId, _doctorUserId,
            RecordAccessLevel.Full, "Upgraded access");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessId.Should().Be(existingAccessId);

        var access = await _db.RecordAccesses.FindAsync(existingAccessId);
        access!.AccessLevel.Should().Be(RecordAccessLevel.Full);
        access.Reason.Should().Be("Upgraded access");
        access.RevokedAt.Should().BeNull();

        // Should not have created a second record
        var count = await _db.RecordAccesses.CountAsync();
        count.Should().Be(1);
    }
}
