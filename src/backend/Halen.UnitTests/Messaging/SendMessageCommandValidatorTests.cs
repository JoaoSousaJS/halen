using Halen.Infrastructure.Persistence;
using FluentAssertions;
using Halen.Application.Messaging.Commands;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class SendMessageCommandValidatorTests
{
    private SendMessageCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new SendMessageCommandValidator();
    }

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "Hello doctor!");
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyContent_Fails()
    {
        var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "");
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [TestMethod]
    public async Task Validate_ContentTooLong_Fails()
    {
        var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('A', 4001));
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [TestMethod]
    public async Task Validate_EmptyUserId_Fails()
    {
        var command = new SendMessageCommand(Guid.Empty, Guid.NewGuid(), "Hello");
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
