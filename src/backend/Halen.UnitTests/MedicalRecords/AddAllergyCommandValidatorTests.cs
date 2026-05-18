using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class AddAllergyCommandValidatorTests
{
    private AddAllergyCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new AddAllergyCommandValidator();
    }

    private static AddAllergyCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        "Penicillin", "Rash and hives", ConditionSeverity.Severe,
        new DateOnly(2023, 1, 15));

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyAllergenName_Fails()
    {
        var command = ValidCommand() with { AllergenName = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AllergenName");
    }

    [TestMethod]
    public async Task Validate_AllergenNameTooLong_Fails()
    {
        var command = ValidCommand() with { AllergenName = new string('x', 201) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AllergenName");
    }

    [TestMethod]
    public async Task Validate_ReactionTooLong_Fails()
    {
        var command = ValidCommand() with { Reaction = new string('x', 501) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reaction");
    }

    [TestMethod]
    public async Task Validate_InvalidSeverity_Fails()
    {
        var command = ValidCommand() with { Severity = (ConditionSeverity)99 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Severity");
    }

    [TestMethod]
    public async Task Validate_NullReaction_Passes()
    {
        var command = ValidCommand() with { Reaction = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_NullDateIdentified_Passes()
    {
        var command = ValidCommand() with { DateIdentified = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
