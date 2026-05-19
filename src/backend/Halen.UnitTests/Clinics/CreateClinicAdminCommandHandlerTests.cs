using FluentAssertions;
using Halen.Application.Clinics.Commands;
using Halen.Application.Common;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Clinics;

[TestClass]
public class CreateClinicAdminCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private CreateClinicAdminCommandHandler _handler = null!;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _handler = new CreateClinicAdminCommandHandler(
            _userManagerMock.Object,
            _db,
            Mock.Of<ILogger<CreateClinicAdminCommandHandler>>());

        _clinicId = Guid.NewGuid();
        _db.Clinics.Add(new Clinic { Id = _clinicId, Name = "Test Clinic", Slug = "test", IsActive = true });
        await _db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesClinicAdmin()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Admin1234!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "ClinicAdmin"))
            .ReturnsAsync(IdentityResult.Success);

        var command = new CreateClinicAdminCommand(_clinicId, "admin@clinic.com", "Jane", "Doe", "Admin1234!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeNull();

        _userManagerMock.Verify(m => m.CreateAsync(
            It.Is<User>(u =>
                u.Role == UserRole.ClinicAdmin &&
                u.Status == AccountStatus.Active &&
                u.ClinicId == _clinicId &&
                u.Email == "admin@clinic.com"),
            "Admin1234!"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_NonExistentClinic_ReturnsNotFound()
    {
        var command = new CreateClinicAdminCommand(Guid.NewGuid(), "admin@clinic.com", "Jane", "Doe", "Admin1234!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_InactiveClinic_ReturnsValidation()
    {
        var inactiveClinicId = Guid.NewGuid();
        _db.Clinics.Add(new Clinic { Id = inactiveClinicId, Name = "Inactive", Slug = "inactive", IsActive = false });
        await _db.SaveChangesAsync();

        var command = new CreateClinicAdminCommand(inactiveClinicId, "admin@clinic.com", "Jane", "Doe", "Admin1234!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("inactive");
    }

    [TestMethod]
    public async Task Handle_UserCreationFails_ReturnsValidation()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already taken" }));

        var command = new CreateClinicAdminCommand(_clinicId, "dup@clinic.com", "Dup", "Admin", "Admin1234!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("Email already taken");
    }

    [TestMethod]
    public async Task Handle_RoleAssignmentFails_RollsBackUser()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Admin1234!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "ClinicAdmin"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role does not exist" }));
        _userManagerMock
            .Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new CreateClinicAdminCommand(_clinicId, "role@clinic.com", "Role", "Fail", "Admin1234!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        _userManagerMock.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_DoesNotCreateProfiles()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Admin1234!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "ClinicAdmin"))
            .ReturnsAsync(IdentityResult.Success);

        var command = new CreateClinicAdminCommand(_clinicId, "noprofile@clinic.com", "No", "Profile", "Admin1234!");
        await _handler.Handle(command, CancellationToken.None);

        var doctorProfiles = await _db.DoctorProfiles.ToListAsync();
        var patientProfiles = await _db.PatientProfiles.ToListAsync();
        doctorProfiles.Should().BeEmpty();
        patientProfiles.Should().BeEmpty();
    }
}
