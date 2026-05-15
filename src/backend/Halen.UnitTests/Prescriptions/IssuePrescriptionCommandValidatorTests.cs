using FluentAssertions;
using Halen.Application.Prescriptions.Commands;

namespace Halen.UnitTests.Prescriptions;

[TestClass]
public class IssuePrescriptionCommandValidatorTests
{
    private IssuePrescriptionCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new IssuePrescriptionCommandValidator();
    }

    [TestMethod]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            "Amoxicillin", "500mg", "Twice daily", 3, "CVS Pharmacy");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyDrugName_FailsValidation()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            "", "500mg", "Twice daily", 3, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DrugName");
    }

    [TestMethod]
    public async Task Validate_NegativeRefills_FailsValidation()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            "Amoxicillin", "500mg", "Twice daily", -1, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefillsRemaining");
    }

    [TestMethod]
    public async Task Validate_RefillsExceedMax_FailsValidation()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            "Amoxicillin", "500mg", "Twice daily", 25, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefillsRemaining");
    }

    [TestMethod]
    public async Task Validate_EmptyDoctorUserId_FailsValidation()
    {
        var command = new IssuePrescriptionCommand(
            Guid.Empty, Guid.NewGuid(),
            "Amoxicillin", "500mg", "Twice daily", 3, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DoctorUserId");
    }

    [TestMethod]
    public async Task Validate_NoPharmacy_PassesValidation()
    {
        var command = new IssuePrescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            "Amoxicillin", "500mg", "Twice daily", 0, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
