using FluentAssertions;
using Halen.Application.Auth.Commands;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Halen.UnitTests.Auth;

[TestClass]
public class LoginCommandHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = null!;
    private Mock<SignInManager<User>> _signInManagerMock = null!;
    private Mock<IJwtService> _jwtServiceMock = null!;
    private Mock<IAppDbContext> _dbMock = null!;
    private Mock<IAuditContextProvider> _auditContextMock = null!;
    private Mock<ILogger<LoginCommandHandler>> _loggerMock = null!;
    private LoginCommandHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<User>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>());

        _jwtServiceMock = new Mock<IJwtService>();

        var testDb = Helpers.TestDbFactory.Create();
        _dbMock = new Mock<IAppDbContext>();
        _dbMock.Setup(d => d.AuditLogs).Returns(testDb.AuditLogs);
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _auditContextMock = new Mock<IAuditContextProvider>();
        _auditContextMock.Setup(a => a.IpAddress).Returns("127.0.0.1");

        _loggerMock = new Mock<ILogger<LoginCommandHandler>>();

        _handler = new LoginCommandHandler(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtServiceMock.Object,
            _dbMock.Object,
            _auditContextMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var command = new LoginCommand("jane@example.com", "SecurePass1!");
        var user = new User { Email = command.Email, FirstName = "Jane", LastName = "Doe" };
        const string expectedToken = "jwt-token-xyz";

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(SignInResult.Success);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Patient" });

        _jwtServiceMock
            .Setup(s => s.GenerateToken(user, It.IsAny<IList<string>>()))
            .Returns(expectedToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(expectedToken);
        result.Error.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_UserNotFound_ReturnsFailureWithInvalidCredentials()
    {
        // Arrange
        var command = new LoginCommand("ghost@example.com", "AnyPassword1!");

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.Error.Should().Be("Invalid credentials");
    }

    [TestMethod]
    public async Task Handle_WrongPassword_ReturnsFailureWithInvalidCredentials()
    {
        // Arrange
        var command = new LoginCommand("jane@example.com", "WrongPassword!");
        var user = new User { Email = command.Email };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.Error.Should().Be("Invalid credentials");
    }

    [TestMethod]
    public async Task Handle_AccountLockedOut_ReturnsAccountLockedMessage()
    {
        // Arrange
        var command = new LoginCommand("jane@example.com", "AnyPassword1!");
        var user = new User { Email = command.Email };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(SignInResult.LockedOut);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.Error.Should().Be("Account locked");
    }

    [TestMethod]
    public async Task Handle_SuspendedUser_ReturnsAccountSuspended()
    {
        var command = new LoginCommand("jane@example.com", "SecurePass1!");
        var user = new User { Email = command.Email, Status = AccountStatus.Suspended };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.Error.Should().Be("Account suspended");
        _signInManagerMock.Verify(
            m => m.CheckPasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_PendingReviewUser_AllowsLoginForKycSubmission()
    {
        var command = new LoginCommand("jane@example.com", "SecurePass1!");
        var user = new User { Email = command.Email, Status = AccountStatus.PendingReview };
        const string expectedToken = "jwt-token-pending";

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(SignInResult.Success);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Doctor" });

        _jwtServiceMock
            .Setup(s => s.GenerateToken(user, It.IsAny<IList<string>>()))
            .Returns(expectedToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Token.Should().Be(expectedToken);
    }

    [TestMethod]
    public async Task Handle_SuccessfulLogin_CreatesLoginSuccessAuditLog()
    {
        var command = new LoginCommand("jane@example.com", "SecurePass1!");
        var user = new User { Email = command.Email, FirstName = "Jane", LastName = "Doe" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Patient" });
        _jwtServiceMock.Setup(s => s.GenerateToken(user, It.IsAny<IList<string>>())).Returns("token");

        await _handler.Handle(command, CancellationToken.None);

        _dbMock.Verify(d => d.AuditLogs, Times.AtLeastOnce);
        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Handle_FailedLogin_CreatesLoginFailureAuditLog()
    {
        var command = new LoginCommand("jane@example.com", "WrongPass!");
        var user = new User { Email = command.Email };

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(SignInResult.Failed);

        await _handler.Handle(command, CancellationToken.None);

        _dbMock.Verify(d => d.AuditLogs, Times.AtLeastOnce);
        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Handle_AuditWriteFails_LoginStillSucceeds()
    {
        var command = new LoginCommand("jane@example.com", "SecurePass1!");
        var user = new User { Email = command.Email };

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Patient" });
        _jwtServiceMock.Setup(s => s.GenerateToken(user, It.IsAny<IList<string>>())).Returns("token");
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB failure"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Token.Should().Be("token");
    }
}
