using FluentAssertions;
using Halen.Application.Auth.Commands;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Auth;

[TestClass]
public class RegisterCommandHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = null!;
    private Mock<IJwtService> _jwtServiceMock = null!;
    private Mock<ILogger<RegisterCommandHandler>> _loggerMock = null!;
    private RegisterCommandHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<RegisterCommandHandler>>();

        _handler = new RegisterCommandHandler(
            _userManagerMock.Object,
            _jwtServiceMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task Handle_ValidPatientCommand_ReturnsSuccessWithToken()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "Doe", "jane@example.com", "SecurePass1!", UserRole.Patient);
        const string expectedToken = "jwt-token-abc";

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserRole.Patient.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "Patient" });

        _jwtServiceMock
            .Setup(s => s.GenerateToken(It.IsAny<User>(), It.IsAny<IList<string>>()))
            .Returns(expectedToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(expectedToken);
        result.Error.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_UserManagerCreateFails_ReturnsErrorMessage()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "Doe", "jane@example.com", "weak", UserRole.Patient);
        var identityErrors = new[] { new IdentityError { Description = "Password too short." } };

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.Error.Should().Contain("Password too short.");
    }

    [TestMethod]
    [DataRow(UserRole.Admin)]
    [DataRow(UserRole.Doctor)]
    public async Task Handle_NonPatientRole_ReturnsErrorWithoutCreatingUser(UserRole role)
    {
        var command = new RegisterCommand("Test", "User", "test@example.com", "SecurePass1!", role);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Self-registration is only allowed for patients.");
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_AddToRoleAsyncFails_DeletesUserAndReturnsError()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "Doe", "jane@example.com", "SecurePass1!", UserRole.Patient);
        var roleErrors = new[] { new IdentityError { Description = "Role does not exist." } };

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserRole.Patient.ToString()))
            .ReturnsAsync(IdentityResult.Failed(roleErrors));

        _userManagerMock
            .Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.Error.Should().Be("Account setup failed. Please try again.");

        _userManagerMock.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Once);
    }
}
