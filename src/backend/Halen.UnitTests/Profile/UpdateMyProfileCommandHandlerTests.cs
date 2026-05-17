using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Profile.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Halen.UnitTests.Profile;

[TestClass]
public class UpdateMyProfileCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private UpdateMyProfileCommandHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new UpdateMyProfileCommandHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_UpdatesFirstNameAndLastName()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = userId,
            FirstName = "Old",
            LastName = "Name",
            Email = "user@test.com",
            UserName = "user@test.com",
            Role = UserRole.Patient,
        });
        await _db.SaveChangesAsync();

        var command = new UpdateMyProfileCommand(userId, "New", "Name", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var updated = await _db.Users.FirstAsync(u => u.Id == userId);
        updated.FirstName.Should().Be("New");
        updated.LastName.Should().Be("Name");
    }

    [TestMethod]
    public async Task Handle_PatientRole_UpdatesPatientSpecificFields()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            UserName = "john@test.com",
            Role = UserRole.Patient,
        });

        _db.PatientProfiles.Add(new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DateOfBirth = new DateOnly(1990, 1, 1),
            City = "Old City",
        });
        await _db.SaveChangesAsync();

        var command = new UpdateMyProfileCommand(
            userId, "John", "Doe",
            new DateOnly(1985, 6, 15), "New York");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var profile = await _db.PatientProfiles.FirstAsync(p => p.UserId == userId);
        profile.DateOfBirth.Should().Be(new DateOnly(1985, 6, 15));
        profile.City.Should().Be("New York");
    }

    [TestMethod]
    public async Task Handle_DoctorRole_IgnoresPatientFields()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = userId,
            FirstName = "Dr",
            LastName = "House",
            Email = "house@test.com",
            UserName = "house@test.com",
            Role = UserRole.Doctor,
        });
        await _db.SaveChangesAsync();

        var command = new UpdateMyProfileCommand(
            userId, "Gregory", "House",
            new DateOnly(1985, 6, 15), "Princeton");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        // Name should be updated
        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        user.FirstName.Should().Be("Gregory");
        user.LastName.Should().Be("House");

        // No patient profile should exist — doctor role does not write patient fields
        var patientProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        patientProfile.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_UserNotFound_ReturnsNotFoundError()
    {
        var command = new UpdateMyProfileCommand(Guid.NewGuid(), "John", "Doe", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}
