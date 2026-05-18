using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class AddVitalCommandValidatorTests
{
    private AddVitalCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new AddVitalCommandValidator();
    }

    private static AddVitalCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        VitalType.HeartRate, 72m, null, "bpm",
        DateTime.UtcNow, VitalSource.Manual, null);

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_InvalidVitalType_Fails()
    {
        var command = ValidCommand() with { VitalType = (VitalType)99 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VitalType");
    }

    [TestMethod]
    public async Task Validate_ZeroValue_Fails()
    {
        var command = ValidCommand() with { Value = 0 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [TestMethod]
    public async Task Validate_NegativeValue_Fails()
    {
        var command = ValidCommand() with { Value = -1 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [TestMethod]
    public async Task Validate_EmptyUnit_Fails()
    {
        var command = ValidCommand() with { Unit = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Unit");
    }

    [TestMethod]
    public async Task Validate_UnitTooLong_Fails()
    {
        var command = ValidCommand() with { Unit = new string('x', 21) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Unit");
    }

    [TestMethod]
    public async Task Validate_MeasuredAtInFarFuture_Fails()
    {
        var command = ValidCommand() with { MeasuredAt = DateTime.UtcNow.AddMinutes(10) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MeasuredAt");
    }

    [TestMethod]
    public async Task Validate_MeasuredAtInPast_Passes()
    {
        var command = ValidCommand() with { MeasuredAt = DateTime.UtcNow.AddDays(-1) };
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
