using FluentAssertions;
using Halen.Application.Auth.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.Auth;

[TestClass]
public class RegisterCommandValidatorTests
{
    private RegisterCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new RegisterCommandValidator();
    }

    [TestMethod]
    public async Task Validate_ValidPatientCommand_PassesValidation()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "Doe", "jane@example.com", "SecurePass1!", UserRole.Patient);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(UserRole.PlatformAdmin)]
    [DataRow(UserRole.Doctor)]
    public async Task Validate_NonPatientRole_FailsWithSelfRegistrationMessage(UserRole role)
    {
        var command = new RegisterCommand("Test", "User", "test@example.com", "SecurePass1!", role);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.ErrorMessage == "Self-registration is only allowed for patients.");
    }

    [TestMethod]
    public async Task Validate_EmptyEmail_FailsValidation()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "Doe", "", "SecurePass1!", UserRole.Patient);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [TestMethod]
    public async Task Validate_PasswordTooShort_FailsValidation()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "Doe", "jane@example.com", "short", UserRole.Patient);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [TestMethod]
    public async Task Validate_InvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "Doe", "not-an-email", "SecurePass1!", UserRole.Patient);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
