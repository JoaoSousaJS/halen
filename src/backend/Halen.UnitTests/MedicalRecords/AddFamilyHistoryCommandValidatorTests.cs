using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class AddFamilyHistoryCommandValidatorTests
{
    private AddFamilyHistoryCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new AddFamilyHistoryCommandValidator();
    }

    private static AddFamilyHistoryCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        "Father", "Diabetes Type 2", 55, "Managed with medication");

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyRelationship_Fails()
    {
        var command = ValidCommand() with { Relationship = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Relationship");
    }

    [TestMethod]
    public async Task Validate_RelationshipTooLong_Fails()
    {
        var command = ValidCommand() with { Relationship = new string('x', 51) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Relationship");
    }

    [TestMethod]
    public async Task Validate_EmptyConditionName_Fails()
    {
        var command = ValidCommand() with { ConditionName = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConditionName");
    }

    [TestMethod]
    public async Task Validate_ConditionNameTooLong_Fails()
    {
        var command = ValidCommand() with { ConditionName = new string('x', 201) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConditionName");
    }

    [TestMethod]
    public async Task Validate_NegativeAgeAtOnset_Fails()
    {
        var command = ValidCommand() with { AgeAtOnset = -1 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AgeAtOnset");
    }

    [TestMethod]
    public async Task Validate_AgeAtOnsetExceeds150_Fails()
    {
        var command = ValidCommand() with { AgeAtOnset = 151 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AgeAtOnset");
    }

    [TestMethod]
    public async Task Validate_AgeAtOnsetNull_Passes()
    {
        var command = ValidCommand() with { AgeAtOnset = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_AgeAtOnsetZero_Passes()
    {
        var command = ValidCommand() with { AgeAtOnset = 0 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_AgeAtOnset150_Passes()
    {
        var command = ValidCommand() with { AgeAtOnset = 150 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_NotesTooLong_Fails()
    {
        var command = ValidCommand() with { Notes = new string('x', 501) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [TestMethod]
    public async Task Validate_NullNotes_Passes()
    {
        var command = ValidCommand() with { Notes = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
