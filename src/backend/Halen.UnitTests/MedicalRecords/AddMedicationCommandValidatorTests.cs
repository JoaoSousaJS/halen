using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class AddMedicationCommandValidatorTests
{
    private AddMedicationCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new AddMedicationCommandValidator();
    }

    private static AddMedicationCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        "Amoxicillin", "500mg", "Twice daily",
        DateOnly.FromDateTime(DateTime.UtcNow), null, "Dr. House", null);

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyMedicationName_Fails()
    {
        var command = ValidCommand() with { MedicationName = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MedicationName");
    }

    [TestMethod]
    public async Task Validate_MedicationNameTooLong_Fails()
    {
        var command = ValidCommand() with { MedicationName = new string('x', 201) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MedicationName");
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
    public async Task Validate_PrescribedByNameTooLong_Fails()
    {
        var command = ValidCommand() with { PrescribedByName = new string('x', 201) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrescribedByName");
    }

    [TestMethod]
    public async Task Validate_NullOptionalFields_Passes()
    {
        var command = ValidCommand() with
        {
            StartDate = null, EndDate = null,
            PrescribedByName = null, LinkedPrescriptionId = null
        };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
