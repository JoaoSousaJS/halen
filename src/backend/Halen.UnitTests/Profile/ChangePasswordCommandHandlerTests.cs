using FluentAssertions;
using Halen.Application.Profile.Commands;
using Halen.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Halen.UnitTests.Profile;

[TestClass]
public class ChangePasswordCommandHandlerTests
{
    private Mock<UserManager<User>> _userManagerMock = null!;
    private ChangePasswordCommandHandler _handler = null!;
    private readonly Guid _userId = Guid.NewGuid();

    [TestInitialize]
    public void Initialize()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _handler = new ChangePasswordCommandHandler(_userManagerMock.Object);
    }

    [TestMethod]
    public async Task Handle_ValidPasswordChange_ReturnsSuccess()
    {
        var user = new User
        {
            Id = _userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            UserName = "john@test.com",
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync(_userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.ChangePasswordAsync(user, "OldPass123", "NewPass456"))
            .ReturnsAsync(IdentityResult.Success);

        var command = new ChangePasswordCommand(_userId, "OldPass123", "NewPass456");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();

        _userManagerMock.Verify(m => m.ChangePasswordAsync(user, "OldPass123", "NewPass456"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WrongCurrentPassword_ReturnsError()
    {
        var user = new User
        {
            Id = _userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            UserName = "john@test.com",
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync(_userId.ToString()))
            .ReturnsAsync(user);

        var identityErrors = new[] { new IdentityError { Description = "Incorrect password." } };
        _userManagerMock
            .Setup(m => m.ChangePasswordAsync(user, "WrongPass", "NewPass456"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var command = new ChangePasswordCommand(_userId, "WrongPass", "NewPass456");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Incorrect password.");
    }

    [TestMethod]
    public async Task Handle_UserNotFound_ReturnsError()
    {
        _userManagerMock
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass123", "NewPass456");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("User not found");

        _userManagerMock.Verify(
            m => m.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
