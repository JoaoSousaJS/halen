using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class RevokeRecordAccessCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private RevokeRecordAccessCommandHandler _handler = null!;
    private Guid _adminUserId;
    private Guid _doctorUserId;
    private Guid _accessId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _adminUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        _accessId = Guid.NewGuid();
        var patientProfileId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _adminUserId, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.PlatformAdmin },
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor },
            new User { Id = patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = patientProfileId, UserId = patientUserId });

        _db.RecordAccesses.Add(new RecordAccess
        {
            Id = _accessId,
            PatientProfileId = patientProfileId,
            GrantedToUserId = _doctorUserId,
            AccessLevel = RecordAccessLevel.Full,
            GrantedAt = DateTime.UtcNow.AddDays(-7),
            GrantedByUserId = _adminUserId,
        });

        await _db.SaveChangesAsync();

        _handler = new RevokeRecordAccessCommandHandler(
            _db, Mock.Of<ILogger<RevokeRecordAccessCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_RevokesAccess()
    {
        var command = new RevokeRecordAccessCommand(
            _adminUserId, _accessId, "No longer needed");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var access = await _db.RecordAccesses.FindAsync(_accessId);
        access.Should().NotBeNull();
        access!.AccessLevel.Should().Be(RecordAccessLevel.Revoked);
        access.RevokedAt.Should().NotBeNull();
        access.Reason.Should().Be("No longer needed");
    }

    [TestMethod]
    public async Task Handle_NotAdmin_ReturnsForbidden()
    {
        var command = new RevokeRecordAccessCommand(
            _doctorUserId, _accessId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
        result.Error.Should().Contain("platform administrator");
    }

    [TestMethod]
    public async Task Handle_AccessNotFound_ReturnsNotFound()
    {
        var command = new RevokeRecordAccessCommand(
            _adminUserId, Guid.NewGuid(), null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("not found");
    }
}
