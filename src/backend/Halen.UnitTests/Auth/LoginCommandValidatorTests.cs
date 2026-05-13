using FluentAssertions;
using Halen.Application.Auth.Commands;

namespace Halen.UnitTests.Auth;

[TestClass]
public class LoginCommandValidatorTests
{
    private LoginCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new LoginCommandValidator();
    }

    [TestMethod]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new LoginCommand("jane@example.com", "SecurePass1!");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyEmail_FailsValidation()
    {
        // Arrange
        var command = new LoginCommand("", "SecurePass1!");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [TestMethod]
    public async Task Validate_InvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var command = new LoginCommand("not-an-email", "SecurePass1!");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [TestMethod]
    public async Task Validate_EmptyPassword_FailsValidation()
    {
        // Arrange
        var command = new LoginCommand("jane@example.com", "");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
