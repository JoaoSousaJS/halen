using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Messaging.Queries;

public record GetThreadMessagesQuery(
    Guid UserId,
    Guid ThreadId,
    int Page,
    int PageSize) : IRequest<GetThreadMessagesResult>;

public record GetThreadMessagesResult(
    bool Success,
    MessageDto[] Messages = null!,
    int TotalCount = 0,
    string? Error = null,
    ErrorKind? Kind = null);

public record MessageDto(
    Guid Id,
    string SenderName,
    UserRole SenderRole,
    Guid SenderUserId,
    string Content,
    MessageType MessageType,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTime CreatedAt,
    AttachmentDto[] Attachments);

public record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    MessageAttachmentType AttachmentType);
