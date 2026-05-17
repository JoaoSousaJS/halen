using FluentAssertions;
using Halen.Application.Clinics.Commands;
using Halen.Application.Common;
using Halen.Application.Interfaces;
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
public class CreateUserInClinicCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private TestTenantContext _tenantContext = null!;
    private CreateUserInClinicCommandHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tenantContext = new TestTenantContext();
        _db = TestDbFactory.Create(_tenantContext);

        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _handler = new CreateUserInClinicCommandHandler(
            _userManagerMock.Object,
            _db,
            _tenantContext,
            Mock.Of<ILogger<CreateUserInClinicCommandHandler>>());
    }

    [TestMethod]
    public async Task Handle_ValidDoctorCommand_CreatesUserAndDoctorProfile()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Temp1234!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Doctor"))
            .ReturnsAsync(IdentityResult.Success);

        var command = new CreateUserInClinicCommand("doc@test.com", "John", "Smith", "Temp1234!", UserRole.Doctor);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeNull();

        var profiles = await _db.DoctorProfiles.ToListAsync();
        profiles.Should().HaveCount(1);
        profiles[0].ClinicId.Should().Be(TestTenantContext.DefaultClinicId);
    }

    [TestMethod]
    public async Task Handle_ValidPatientCommand_CreatesUserAndPatientProfile()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Temp1234!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Patient"))
            .ReturnsAsync(IdentityResult.Success);

        var command = new CreateUserInClinicCommand("pat@test.com", "Jane", "Doe", "Temp1234!", UserRole.Patient);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var profiles = await _db.PatientProfiles.ToListAsync();
        profiles.Should().HaveCount(1);
        profiles[0].ClinicId.Should().Be(TestTenantContext.DefaultClinicId);
    }

    [TestMethod]
    public async Task Handle_UserCreationFails_ReturnsError()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already taken" }));

        var command = new CreateUserInClinicCommand("dup@test.com", "Dup", "User", "Temp1234!", UserRole.Patient);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("Email already taken");
    }

    [TestMethod]
    public async Task Handle_RoleAssignmentFails_DeletesUserAndReturnsError()
    {
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Temp1234!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Patient"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role does not exist" }));
        _userManagerMock
            .Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var command = new CreateUserInClinicCommand("role@test.com", "Role", "Fail", "Temp1234!", UserRole.Patient);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("Role does not exist");
        _userManagerMock.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Once);
    }
}
