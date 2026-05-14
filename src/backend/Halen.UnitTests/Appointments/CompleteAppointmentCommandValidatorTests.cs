using FluentAssertions;
using Halen.Application.Appointments.Commands;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class CompleteAppointmentCommandValidatorTests
{
    private CompleteAppointmentCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new CompleteAppointmentCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), "Patient is healthy");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_NullNotes_Passes()
    {
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new CompleteAppointmentCommand(Guid.Empty, Guid.NewGuid(), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [TestMethod]
    public async Task Validate_EmptyAppointmentId_Fails()
    {
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), Guid.Empty, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AppointmentId");
    }

    [TestMethod]
    public async Task Validate_NotesTooLong_Fails()
    {
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 2001));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }
}
