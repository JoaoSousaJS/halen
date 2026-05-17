using FluentAssertions;
using Halen.Application.Clinics.Commands;
using Halen.Application.Common;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Halen.UnitTests.Clinics;

[TestClass]
public class UpdateClinicCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private UpdateClinicCommandHandler _handler = null!;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new UpdateClinicCommandHandler(_db);

        _clinicId = Guid.NewGuid();
        _db.Clinics.Add(new Clinic { Id = _clinicId, Name = "Original", Slug = "original", IsActive = true });
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Active",
            LastName = "User",
            Email = "user@test.com",
            UserName = "user@test.com",
            Role = UserRole.Patient,
            ClinicId = _clinicId,
            Status = AccountStatus.Active,
        });
        await _db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Handle_UpdateName_Succeeds()
    {
        var command = new UpdateClinicCommand(_clinicId, "New Name", true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var clinic = await _db.Clinics.FindAsync(_clinicId);
        clinic!.Name.Should().Be("New Name");
    }

    // Deactivation + user suspension test lives in integration tests — ExecuteUpdateAsync
    // requires a relational provider (not supported by InMemory).

    [TestMethod]
    public async Task Handle_NonExistentClinic_ReturnsNotFound()
    {
        var command = new UpdateClinicCommand(Guid.NewGuid(), "Name", true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}
