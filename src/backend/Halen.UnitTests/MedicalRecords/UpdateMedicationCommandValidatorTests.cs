using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class UpdateMedicationCommandValidatorTests
{
    private UpdateMedicationCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new UpdateMedicationCommandValidator();
    }

    private static UpdateMedicationCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        "500mg", "Twice daily", null, true);

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
    public async Task Validate_EmptyMedicationId_Fails()
    {
        var command = ValidCommand() with { MedicationId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MedicationId");
    }

    [TestMethod]
    public async Task Validate_EmptyDosage_Fails()
    {
        var command = ValidCommand() with { Dosage = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Dosage");
    }

    [TestMethod]
    public async Task Validate_DosageTooLong_Fails()
    {
        var command = ValidCommand() with { Dosage = new string('x', 101) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Dosage");
    }

    [TestMethod]
    public async Task Validate_DosageAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Dosage = new string('x', 100) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyFrequency_Fails()
    {
        var command = ValidCommand() with { Frequency = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Frequency");
    }

    [TestMethod]
    public async Task Validate_FrequencyTooLong_Fails()
    {
        var command = ValidCommand() with { Frequency = new string('x', 101) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Frequency");
    }

    [TestMethod]
    public async Task Validate_FrequencyAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Frequency = new string('x', 100) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
