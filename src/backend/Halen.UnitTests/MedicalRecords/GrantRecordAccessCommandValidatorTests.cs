using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class GrantRecordAccessCommandValidatorTests
{
    private GrantRecordAccessCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new GrantRecordAccessCommandValidator();
    }

    private static GrantRecordAccessCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        RecordAccessLevel.Full, "Doctor needs access for treatment");

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
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
    public async Task Validate_EmptyGrantToUserId_Fails()
    {
        var command = ValidCommand() with { GrantToUserId = Guid.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GrantToUserId");
    }

    [TestMethod]
    public async Task Validate_InvalidAccessLevel_Fails()
    {
        var command = ValidCommand() with { AccessLevel = (RecordAccessLevel)99 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccessLevel");
    }

    [TestMethod]
    public async Task Validate_ReasonTooLong_Fails()
    {
        var command = ValidCommand() with { Reason = new string('x', 501) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [TestMethod]
    public async Task Validate_NullReason_Passes()
    {
        var command = ValidCommand() with { Reason = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_ReasonAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Reason = new string('x', 500) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
