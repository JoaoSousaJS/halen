using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class AddConditionCommandValidatorTests
{
    private AddConditionCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new AddConditionCommandValidator();
    }

    private static AddConditionCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        "J06.9", "Acute upper respiratory infection", null,
        ConditionSeverity.Mild, ConditionStatus.Active, null, null);

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
    public async Task Validate_EmptyPatientProfileId_Fails()
    {
        var command = ValidCommand() with { PatientProfileId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PatientProfileId");
    }

    [TestMethod]
    public async Task Validate_EmptyIcdCode_Fails()
    {
        var command = ValidCommand() with { IcdCode = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IcdCode");
    }

    [TestMethod]
    public async Task Validate_IcdCodeTooLong_Fails()
    {
        var command = ValidCommand() with { IcdCode = "A12345678901" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IcdCode");
    }

    [TestMethod]
    public async Task Validate_IcdCodeInvalidFormat_Fails()
    {
        var command = ValidCommand() with { IcdCode = "123" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IcdCode");
    }

    [TestMethod]
    [DataRow("A00")]
    [DataRow("J06.9")]
    [DataRow("Z99.1234")]
    public async Task Validate_ValidIcdCodeFormats_Pass(string icdCode)
    {
        var command = ValidCommand() with { IcdCode = icdCode };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyIcdDescription_Fails()
    {
        var command = ValidCommand() with { IcdDescription = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IcdDescription");
    }

    [TestMethod]
    public async Task Validate_IcdDescriptionTooLong_Fails()
    {
        var command = ValidCommand() with { IcdDescription = new string('x', 501) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IcdDescription");
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
}
