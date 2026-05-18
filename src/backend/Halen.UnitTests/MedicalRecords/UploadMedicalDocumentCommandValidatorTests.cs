using FluentAssertions;
using Halen.Application.MedicalRecords.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.MedicalRecords;

[TestClass]
public class UploadMedicalDocumentCommandValidatorTests
{
    private UploadMedicalDocumentCommandValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new UploadMedicalDocumentCommandValidator();
    }

    private static UploadMedicalDocumentCommand ValidCommand() => new(
        Guid.NewGuid(), UserRole.Doctor, Guid.NewGuid(),
        MedicalDocumentType.LabResult, "Blood Test Results",
        "Annual blood work", "blood-test.pdf", "application/pdf",
        1024, Stream.Null, null);

    [TestMethod]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyTitle_Fails()
    {
        var command = ValidCommand() with { Title = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [TestMethod]
    public async Task Validate_TitleTooLong_Fails()
    {
        var command = ValidCommand() with { Title = new string('x', 201) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [TestMethod]
    public async Task Validate_DescriptionTooLong_Fails()
    {
        var command = ValidCommand() with { Description = new string('x', 501) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [TestMethod]
    public async Task Validate_EmptyFileName_Fails()
    {
        var command = ValidCommand() with { FileName = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [TestMethod]
    public async Task Validate_FileNameTooLong_Fails()
    {
        var command = ValidCommand() with { FileName = new string('x', 257) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [TestMethod]
    public async Task Validate_EmptyContentType_Fails()
    {
        var command = ValidCommand() with { ContentType = "" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [TestMethod]
    public async Task Validate_InvalidContentType_Fails()
    {
        var command = ValidCommand() with { ContentType = "text/plain" };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [TestMethod]
    [DataRow("application/pdf")]
    [DataRow("image/jpeg")]
    [DataRow("image/png")]
    public async Task Validate_AllowedContentTypes_Pass(string contentType)
    {
        var command = ValidCommand() with { ContentType = contentType };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_ZeroFileSizeBytes_Fails()
    {
        var command = ValidCommand() with { FileSizeBytes = 0 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes");
    }

    [TestMethod]
    public async Task Validate_FileSizeExceeds10MB_Fails()
    {
        var command = ValidCommand() with { FileSizeBytes = 10_485_761 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes");
    }

    [TestMethod]
    public async Task Validate_FileSizeExactly10MB_Passes()
    {
        var command = ValidCommand() with { FileSizeBytes = 10_485_760 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_InvalidDocumentType_Fails()
    {
        var command = ValidCommand() with { DocumentType = (MedicalDocumentType)99 };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentType");
    }

    [TestMethod]
    public async Task Validate_NullDescription_Passes()
    {
        var command = ValidCommand() with { Description = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
