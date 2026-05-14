using FluentAssertions;
using Halen.Application.Appointments.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class CancelAppointmentCommandValidatorTests
{
    private CancelAppointmentCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new CancelAppointmentCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new CancelAppointmentCommand(Guid.NewGuid(), UserRole.Patient, Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new CancelAppointmentCommand(Guid.Empty, UserRole.Patient, Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [TestMethod]
    public async Task Validate_InvalidUserRole_Fails()
    {
        var command = new CancelAppointmentCommand(Guid.NewGuid(), (UserRole)999, Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserRole");
    }

    [TestMethod]
    public async Task Validate_PatientRole_Passes()
    {
        var command = new CancelAppointmentCommand(Guid.NewGuid(), UserRole.Patient, Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyAppointmentId_Fails()
    {
        var command = new CancelAppointmentCommand(Guid.NewGuid(), UserRole.Patient, Guid.Empty);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AppointmentId");
    }
}
