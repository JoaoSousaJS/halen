using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Messaging.Queries;

public record SearchMessagesQuery(
    Guid UserId,
    string Query,
    int Page,
    int PageSize) : IRequest<SearchMessagesResult>;

public record SearchMessagesResult(
    bool Success,
    SearchHitDto[] Hits = null!,
    int TotalCount = 0,
    string? Error = null,
    ErrorKind? Kind = null);

public record SearchHitDto(
    Guid ThreadId,
    string OtherParticipantName,
    Guid MessageId,
    string Content,
    string SenderName,
    DateTime CreatedAt,
    bool HasAttachment);
