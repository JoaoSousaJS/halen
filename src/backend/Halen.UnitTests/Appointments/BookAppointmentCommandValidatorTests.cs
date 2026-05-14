using FluentAssertions;
using Halen.Application.Appointments.Commands;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class BookAppointmentCommandValidatorTests
{
    private BookAppointmentCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new BookAppointmentCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), "Headache");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_PastDate_Fails()
    {
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), "Headache");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ScheduledAt");
    }

    [TestMethod]
    public async Task Validate_EmptyReason_Fails()
    {
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), "");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new BookAppointmentCommand(
            Guid.Empty, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), "Headache");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [TestMethod]
    public async Task Validate_EmptyDoctorId_Fails()
    {
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.Empty, DateTime.UtcNow.AddDays(1), "Headache");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DoctorId");
    }

    [TestMethod]
    public async Task Validate_ReasonTooLong_Fails()
    {
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), new string('x', 501));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}
