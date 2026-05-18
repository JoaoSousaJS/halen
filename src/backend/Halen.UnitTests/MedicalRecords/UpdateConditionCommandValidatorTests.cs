using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class UpdateConditionCommandValidatorTests
{
    private UpdateConditionCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new UpdateConditionCommandValidator();
    }

    private static UpdateConditionCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        ConditionSeverity.Mild, ConditionStatus.Active, "Some notes");

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
    public async Task Validate_EmptyConditionId_Fails()
    {
        var command = ValidCommand() with { ConditionId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConditionId");
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
    public async Task Validate_InvalidStatus_Fails()
    {
        var command = ValidCommand() with { Status = (ConditionStatus)99 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [TestMethod]
    public async Task Validate_ClinicalNotesTooLong_Fails()
    {
        var command = ValidCommand() with { ClinicalNotes = new string('x', 2001) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClinicalNotes");
    }

    [TestMethod]
    public async Task Validate_ClinicalNotesAtMaxLength_Passes()
    {
        var command = ValidCommand() with { ClinicalNotes = new string('x', 2000) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_NullClinicalNotes_Passes()
    {
        var command = ValidCommand() with { ClinicalNotes = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
