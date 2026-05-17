using FluentAssertions;
using Halen.Application.Availability.Commands;

namespace Halen.UnitTests.Availability;

[TestClass]
public class SetDoctorAvailabilityCommandValidatorTests
{
    private SetDoctorAvailabilityCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new SetDoctorAvailabilityCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new SetDoctorAvailabilityCommand(
            Guid.NewGuid(),
            new List<AvailabilitySlotDto>
            {
                new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_StartTimeGreaterThanOrEqualToEndTime_Fails()
    {
        var command = new SetDoctorAvailabilityCommand(
            Guid.NewGuid(),
            new List<AvailabilitySlotDto>
            {
                new(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(9, 0)),
            });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("StartTime must be before EndTime"));
    }

    [TestMethod]
    public async Task Validate_StartTimeEqualsEndTime_Fails()
    {
        var command = new SetDoctorAvailabilityCommand(
            Guid.NewGuid(),
            new List<AvailabilitySlotDto>
            {
                new(DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(10, 0)),
            });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("StartTime must be before EndTime"));
    }

    [TestMethod]
    public async Task Validate_InvalidDayOfWeek_Fails()
    {
        var command = new SetDoctorAvailabilityCommand(
            Guid.NewGuid(),
            new List<AvailabilitySlotDto>
            {
                new((DayOfWeek)99, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("DayOfWeek"));
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new SetDoctorAvailabilityCommand(
            Guid.Empty,
            new List<AvailabilitySlotDto>
            {
                new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
