using FluentAssertions;
using Halen.Application.Profile.Commands;

namespace Halen.UnitTests.Profile;

[TestClass]
public class ChangePasswordCommandValidatorTests
{
    private ChangePasswordCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new ChangePasswordCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass123", "NewPass456");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyCurrentPassword_Fails()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "", "NewPass456");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
    }

    [TestMethod]
    public async Task Validate_EmptyNewPassword_Fails()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass123", "");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new ChangePasswordCommand(Guid.Empty, "OldPass123", "NewPass456");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [TestMethod]
    public async Task Validate_ShortNewPassword_Fails()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass123", "short");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" &&
            e.ErrorMessage.Contains("at least 8 characters"));
    }

    [TestMethod]
    public async Task Validate_SameOldAndNewPassword_Fails()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "SamePass123", "SamePass123");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "NewPassword" &&
            e.ErrorMessage.Contains("different from current password"));
    }
}
