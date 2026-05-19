using FluentValidation;

namespace Halen.Application.Messaging.Commands;

public class SendAttachmentCommandValidator : AbstractValidator<SendAttachmentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/png", "image/jpeg", "application/pdf", "audio/webm"
    ];

    private static readonly Dictionary<string, string[]> ContentTypeToExtensions = new()
    {
        ["image/png"] = [".png"],
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["application/pdf"] = [".pdf"],
        ["audio/webm"] = [".webm"],
    };

    public SendAttachmentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ThreadId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("File type not allowed. Allowed: PNG, JPEG, PDF, WebM audio");
        RuleFor(x => x)
            .Must(x => ExtensionMatchesContentType(x.FileName, x.ContentType))
            .WithMessage("File extension does not match content type")
            .When(x => !string.IsNullOrEmpty(x.FileName) && !string.IsNullOrEmpty(x.ContentType));
        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(10 * 1024 * 1024)
            .WithMessage("File size must not exceed 10 MB");
    }

    private static bool ExtensionMatchesContentType(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ContentTypeToExtensions.TryGetValue(contentType, out var allowed)
            && allowed.Contains(ext);
    }
}
