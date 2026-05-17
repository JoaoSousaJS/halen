using FluentAssertions;
using Halen.Application.Consultations.Commands;

namespace Halen.UnitTests.Consultations;

[TestClass]
public class StartConsultationCommandValidatorTests
{
    private StartConsultationCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new StartConsultationCommandValidator();

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new StartConsultationCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new StartConsultationCommand(Guid.Empty, Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [TestMethod]
    public async Task Validate_EmptyAppointmentId_Fails()
    {
        var command = new StartConsultationCommand(Guid.NewGuid(), Guid.Empty);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AppointmentId");
    }
}
