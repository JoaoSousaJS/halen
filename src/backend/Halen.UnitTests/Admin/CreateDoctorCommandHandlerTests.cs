using FluentAssertions;
using Halen.Application.Admin.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Admin;

[TestClass]
public class CreateDoctorCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private TestTenantContext _tenantContext = null!;
    private CreateDoctorCommandHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _tenantContext = new TestTenantContext();

        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _handler = new CreateDoctorCommandHandler(
            _userManagerMock.Object,
            _db,
            _tenantContext,
            Mock.Of<ILogger<CreateDoctorCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesDoctorProfile()
    {
        var command = new CreateDoctorCommand(
            "Jane", "Smith", "jane@clinic.com", "SecurePass1!",
            "Cardiology", "LIC-100", 200, 12);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Doctor"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.DoctorId.Should().NotBeNull();
        result.Error.Should().BeNull();

        var profile = await _db.DoctorProfiles.FirstOrDefaultAsync(d => d.Id == result.DoctorId);
        profile.Should().NotBeNull();
        profile!.Specialty.Should().Be("Cardiology");
        profile.LicenseNumber.Should().Be("LIC-100");
        profile.ConsultationFee.Should().Be(200);
        profile.YearsOfExperience.Should().Be(12);
        profile.ClinicId.Should().Be(TestTenantContext.DefaultClinicId);
    }

    [TestMethod]
    public async Task Handle_DuplicateEmail_ReturnsError()
    {
        var command = new CreateDoctorCommand(
            "Jane", "Smith", "duplicate@clinic.com", "SecurePass1!",
            "Cardiology", "LIC-100", 200, 12);

        var identityErrors = new[] { new IdentityError { Description = "Email 'duplicate@clinic.com' is already taken." } };

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.DoctorId.Should().BeNull();
        result.Error.Should().Contain("already taken");
    }

    [TestMethod]
    public async Task Handle_ValidCommand_AssignsDoctorRole()
    {
        var command = new CreateDoctorCommand(
            "Bob", "Jones", "bob@clinic.com", "SecurePass1!",
            "Dermatology", "LIC-200", 150, 7);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Doctor"))
            .ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(command, CancellationToken.None);

        _userManagerMock.Verify(
            m => m.AddToRoleAsync(
                It.Is<User>(u => u.Email == "bob@clinic.com" && u.Role == UserRole.Doctor),
                "Doctor"),
            Times.Once);
    }
}
