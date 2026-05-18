using FluentAssertions;
using Halen.Application.Reviews;
using Halen.Application.Reviews.Commands;

namespace Halen.UnitTests.Reviews;

[TestClass]
public class SubmitReviewCommandValidatorTests
{
    private SubmitReviewCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize() => _validator = new SubmitReviewCommandValidator();

    private static SubmitReviewCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 5, "Great doctor", "Very thorough", ["listens"]);

    [TestMethod]
    public async Task ValidCommand_Passes()
    {
        var command = ValidCommand();

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task EmptyAppointmentId_Fails()
    {
        var command = ValidCommand() with { AppointmentId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AppointmentId");
    }

    [TestMethod]
    public async Task RatingBelowOne_Fails()
    {
        var command = ValidCommand() with { Rating = 0 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rating");
    }

    [TestMethod]
    public async Task RatingAboveFive_Fails()
    {
        var command = ValidCommand() with { Rating = 6 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rating");
    }

    [TestMethod]
    public async Task EmptyTitle_Fails()
    {
        var command = ValidCommand() with { Title = "" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [TestMethod]
    public async Task TitleTooShort_Fails()
    {
        var command = ValidCommand() with { Title = "Ab" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [TestMethod]
    public async Task TitleTooLong_Fails()
    {
        var command = ValidCommand() with { Title = new string('A', 121) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [TestMethod]
    public async Task BodyTooLong_Fails()
    {
        var command = ValidCommand() with { Body = new string('A', 601) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [TestMethod]
    public async Task TooManyTags_Fails()
    {
        var command = ValidCommand() with
        {
            Tags = ["listens", "thorough", "on time", "calm bedside manner",
                     "clear explanations", "sends follow-up notes", "booking flexibility"]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tags");
    }

    [TestMethod]
    public async Task InvalidTag_Fails()
    {
        var command = ValidCommand() with { Tags = ["nonexistent tag"] };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Tags"));
    }
}
