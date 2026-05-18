using FluentAssertions;
using Halen.Application.Reviews.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.Reviews;

[TestClass]
public class ModerateReviewCommandValidatorTests
{
    private ModerateReviewCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new ModerateReviewCommandValidator();

    private static ModerateReviewCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), ReviewModerationStatus.Approved);

    [TestMethod]
    public async Task ValidCommand_Passes()
    {
        var command = ValidCommand();

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task DecisionIsPending_Fails()
    {
        var command = ValidCommand() with { Decision = ReviewModerationStatus.Pending };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Decision");
    }

    [TestMethod]
    public async Task InvalidDecision_Fails()
    {
        var command = ValidCommand() with { Decision = (ReviewModerationStatus)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Decision");
    }
}
