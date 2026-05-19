using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Messaging.Queries;

public record DownloadAttachmentQuery(
    Guid UserId,
    Guid ThreadId,
    Guid AttachmentId) : IRequest<DownloadAttachmentResult>;

public record DownloadAttachmentResult(
    bool Success,
    Stream? FileStream = null,
    string? ContentType = null,
    string? FileName = null,
    string? Error = null,
    ErrorKind? Kind = null);
