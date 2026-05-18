using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class UpdateAllergyCommandValidatorTests
{
    private UpdateAllergyCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new UpdateAllergyCommandValidator();
    }

    private static UpdateAllergyCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        "Rash and hives", ConditionSeverity.Severe, true);

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyCallerUserId_Fails()
    {
        var command = ValidCommand() with { CallerUserId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CallerUserId");
    }

    [TestMethod]
    public async Task Validate_EmptyAllergyId_Fails()
    {
        var command = ValidCommand() with { AllergyId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AllergyId");
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
    public async Task Validate_ReactionAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Reaction = new string('x', 500) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_NullReaction_Passes()
    {
        var command = ValidCommand() with { Reaction = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_InvalidSeverity_Fails()
    {
        var command = ValidCommand() with { Severity = (ConditionSeverity)99 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Severity");
    }
}
