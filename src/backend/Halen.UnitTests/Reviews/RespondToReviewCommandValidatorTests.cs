using FluentAssertions;
using Halen.Application.Reviews.Commands;

namespace Halen.UnitTests.Reviews;

[TestClass]
public class RespondToReviewCommandValidatorTests
{
    private RespondToReviewCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new RespondToReviewCommandValidator();

    private static RespondToReviewCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Thank you for your feedback");

    [TestMethod]
    public async Task ValidCommand_Passes()
    {
        var command = ValidCommand();

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task EmptyResponse_Fails()
    {
        var command = ValidCommand() with { Response = "" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Response");
    }

    [TestMethod]
    public async Task ResponseTooShort_Fails()
    {
        var command = ValidCommand() with { Response = "Ok" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Response");
    }

    [TestMethod]
    public async Task ResponseTooLong_Fails()
    {
        var command = ValidCommand() with { Response = new string('A', 601) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Response");
    }
}
