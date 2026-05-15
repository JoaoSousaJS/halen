using FluentValidation;
using Halen.Domain.Enums;

namespace Halen.Application.Doctor.Commands;

public class SubmitKycDocumentsCommandValidator : AbstractValidator<SubmitKycDocumentsCommand>
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "application/pdf"];
    private const long MaxFileSizeBytes = 10_485_760; // 10 MB

    public SubmitKycDocumentsCommandValidator()
    {
        RuleFor(x => x.Documents)
            .NotEmpty().WithMessage("At least one document is required.");

        RuleFor(x => x.Documents)
            .Must(docs => docs.Select(d => d.DocumentType).Distinct().Count() == docs.Count)
            .When(x => x.Documents.Count > 0)
            .WithMessage("Duplicate document types are not allowed.");

        RuleForEach(x => x.Documents).ChildRules(doc =>
        {
            doc.RuleFor(d => d.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(256);

            doc.RuleFor(d => d.ContentType)
                .Must(ct => AllowedContentTypes.Contains(ct))
                .WithMessage("Content type must be image/jpeg, image/png, or application/pdf.");

            doc.RuleFor(d => d.FileSizeBytes)
                .GreaterThan(0).WithMessage("File must not be empty.")
                .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("File must not exceed 10 MB.");

            doc.RuleFor(d => d.DocumentType)
                .IsInEnum().WithMessage("Invalid document type.");
        });
    }
}
