using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class UpdateFamilyHistoryCommandValidatorTests
{
    private UpdateFamilyHistoryCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new UpdateFamilyHistoryCommandValidator();
    }

    private static UpdateFamilyHistoryCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        "Type 2 Diabetes", 55, "Managed with insulin");

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
    public async Task Validate_EmptyFamilyHistoryId_Fails()
    {
        var command = ValidCommand() with { FamilyHistoryId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FamilyHistoryId");
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
    public async Task Validate_ConditionNameAtMaxLength_Passes()
    {
        var command = ValidCommand() with { ConditionName = new string('x', 200) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_AgeAtOnsetNegative_Fails()
    {
        var command = ValidCommand() with { AgeAtOnset = -1 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AgeAtOnset");
    }

    [TestMethod]
    public async Task Validate_AgeAtOnsetTooHigh_Fails()
    {
        var command = ValidCommand() with { AgeAtOnset = 151 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AgeAtOnset");
    }

    [TestMethod]
    public async Task Validate_AgeAtOnsetAtBoundaries_Passes()
    {
        var commandZero = ValidCommand() with { AgeAtOnset = 0 };
        var resultZero = await _validator.ValidateAsync(commandZero);
        resultZero.IsValid.Should().BeTrue();

        var commandMax = ValidCommand() with { AgeAtOnset = 150 };
        var resultMax = await _validator.ValidateAsync(commandMax);
        resultMax.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_NullAgeAtOnset_Passes()
    {
        var command = ValidCommand() with { AgeAtOnset = null };
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
    public async Task Validate_NotesAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Notes = new string('x', 500) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_NullNotes_Passes()
    {
        var command = ValidCommand() with { Notes = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
