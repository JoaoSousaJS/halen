using Halen.Infrastructure.Persistence;
using FluentAssertions;
using Halen.Application.Messaging.Commands;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class SendAttachmentCommandValidatorTests
{
    private SendAttachmentCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new SendAttachmentCommandValidator();
    }

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var command = new SendAttachmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "photo.png", "image/png", 1024, Stream.Null);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyFileName_Fails()
    {
        var command = new SendAttachmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "", "image/png", 1024, Stream.Null);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [TestMethod]
    public async Task Validate_DisallowedContentType_Fails()
    {
        var command = new SendAttachmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "script.js", "application/javascript", 1024, Stream.Null);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [TestMethod]
    public async Task Validate_OversizedFile_Fails()
    {
        var command = new SendAttachmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "huge.png", "image/png", 11 * 1024 * 1024, Stream.Null);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes");
    }

    [TestMethod]
    public async Task Validate_ZeroFileSize_Fails()
    {
        var command = new SendAttachmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "empty.png", "image/png", 0, Stream.Null);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes");
    }

    [TestMethod]
    public async Task Validate_PdfAllowed_Passes()
    {
        var command = new SendAttachmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "results.pdf", "application/pdf", 5000, Stream.Null);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
