using FluentAssertions;
using Halen.Application.Consultations.Commands;

namespace Halen.UnitTests.Consultations;

[TestClass]
public class EndConsultationCommandValidatorTests
{
    private EndConsultationCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new EndConsultationCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommandWithNotes_Passes()
    {
        var command = new EndConsultationCommand(Guid.NewGuid(), Guid.NewGuid(), "Some notes");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_ValidCommandWithoutNotes_Passes()
    {
        var command = new EndConsultationCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new EndConsultationCommand(Guid.Empty, Guid.NewGuid(), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [TestMethod]
    public async Task Validate_EmptyAppointmentId_Fails()
    {
        var command = new EndConsultationCommand(Guid.NewGuid(), Guid.Empty, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AppointmentId");
    }

    [TestMethod]
    public async Task Validate_NotesTooLong_Fails()
    {
        var command = new EndConsultationCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 5001));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }
}
