using FluentAssertions;
using Halen.Application.Profile.Commands;

namespace Halen.UnitTests.Profile;

[TestClass]
public class UpdateMyProfileCommandValidatorTests
{
    private UpdateMyProfileCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new UpdateMyProfileCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new UpdateMyProfileCommand(
            Guid.NewGuid(), "John", "Doe",
            new DateOnly(1990, 5, 15), "New York");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyFirstName_Fails()
    {
        var command = new UpdateMyProfileCommand(
            Guid.NewGuid(), "", "Doe", null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [TestMethod]
    public async Task Validate_EmptyLastName_Fails()
    {
        var command = new UpdateMyProfileCommand(
            Guid.NewGuid(), "John", "", null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [TestMethod]
    public async Task Validate_FutureDateOfBirth_Fails()
    {
        var command = new UpdateMyProfileCommand(
            Guid.NewGuid(), "John", "Doe",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "DateOfBirth" &&
            e.ErrorMessage.Contains("must be in the past"));
    }

    [TestMethod]
    public async Task Validate_NullDateOfBirth_Passes()
    {
        var command = new UpdateMyProfileCommand(
            Guid.NewGuid(), "John", "Doe", null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
