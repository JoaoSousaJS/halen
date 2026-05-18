using FluentValidation;

namespace Halen.Application.Messaging.Commands;

public class SendAttachmentCommandValidator : AbstractValidator<SendAttachmentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/png", "image/jpeg", "application/pdf", "audio/webm"
    ];

    public SendAttachmentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ThreadId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("File type not allowed. Allowed: PNG, JPEG, PDF, WebM audio");
        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(10 * 1024 * 1024)
            .WithMessage("File size must not exceed 10 MB");
    }
}
