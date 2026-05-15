using FluentAssertions;
using Halen.Application.Doctor.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.Doctor;

[TestClass]
public class SubmitKycDocumentsCommandValidatorTests
{
    private readonly SubmitKycDocumentsCommandValidator _validator = new();

    private static List<KycDocumentUpload> ValidDocuments() =>
    [
        new(KycDocumentType.LicensePhoto, "license.jpg", "image/jpeg", 1024, "/uploads/license.jpg"),
        new(KycDocumentType.MedicalCertificate, "cert.pdf", "application/pdf", 2048, "/uploads/cert.pdf"),
        new(KycDocumentType.IdentityProof, "id.png", "image/png", 512, "/uploads/id.png"),
    ];

    [TestMethod]
    public void ValidInput_Passes()
    {
        var command = new SubmitKycDocumentsCommand(Guid.NewGuid(), ValidDocuments());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void EmptyDocumentsList_Fails()
    {
        var command = new SubmitKycDocumentsCommand(Guid.NewGuid(), []);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void InvalidContentType_Fails()
    {
        var docs = new List<KycDocumentUpload>
        {
            new(KycDocumentType.LicensePhoto, "license.txt", "text/plain", 1024, "/uploads/license.txt"),
        };
        var command = new SubmitKycDocumentsCommand(Guid.NewGuid(), docs);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Content type"));
    }

    [TestMethod]
    public void FileTooLarge_Fails()
    {
        var docs = new List<KycDocumentUpload>
        {
            new(KycDocumentType.LicensePhoto, "license.jpg", "image/jpeg", 11_000_000, "/uploads/big.jpg"),
        };
        var command = new SubmitKycDocumentsCommand(Guid.NewGuid(), docs);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("10 MB"));
    }

    [TestMethod]
    public void DuplicateDocumentTypes_Fails()
    {
        var docs = new List<KycDocumentUpload>
        {
            new(KycDocumentType.LicensePhoto, "license1.jpg", "image/jpeg", 1024, "/uploads/1.jpg"),
            new(KycDocumentType.LicensePhoto, "license2.jpg", "image/jpeg", 1024, "/uploads/2.jpg"),
        };
        var command = new SubmitKycDocumentsCommand(Guid.NewGuid(), docs);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Duplicate"));
    }
}
