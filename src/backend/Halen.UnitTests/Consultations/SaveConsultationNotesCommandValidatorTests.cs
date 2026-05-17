using FluentAssertions;
using Halen.Application.Consultations.Commands;

namespace Halen.UnitTests.Consultations;

[TestClass]
public class SaveConsultationNotesCommandValidatorTests
{
    private SaveConsultationNotesCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new SaveConsultationNotesCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new SaveConsultationNotesCommand(Guid.NewGuid(), Guid.NewGuid(), "Patient notes");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new SaveConsultationNotesCommand(Guid.Empty, Guid.NewGuid(), "Notes");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [TestMethod]
    public async Task Validate_EmptyAppointmentId_Fails()
    {
        var command = new SaveConsultationNotesCommand(Guid.NewGuid(), Guid.Empty, "Notes");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AppointmentId");
    }

    [TestMethod]
    public async Task Validate_NotesTooLong_Fails()
    {
        var command = new SaveConsultationNotesCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 5001));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }
}
