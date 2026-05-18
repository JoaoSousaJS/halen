using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class UploadMedicalDocumentCommandValidator : AbstractValidator<UploadMedicalDocumentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png"
    ];

    public UploadMedicalDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("ContentType must be application/pdf, image/jpeg, or image/png.");
        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(10_485_760)
            .WithMessage("File size must not exceed 10 MB.");
        RuleFor(x => x.DocumentType).IsInEnum();
    }
}
