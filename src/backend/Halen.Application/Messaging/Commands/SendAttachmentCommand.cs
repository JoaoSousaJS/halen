using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Messaging.Commands;

public record SendAttachmentCommand(
    Guid UserId,
    Guid ThreadId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream) : IRequest<SendAttachmentResult>;

public record SendAttachmentResult(
    bool Success,
    Guid? MessageId = null,
    string? Error = null,
    ErrorKind? Kind = null);
