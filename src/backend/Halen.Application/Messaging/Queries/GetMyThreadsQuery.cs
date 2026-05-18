using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Messaging.Queries;

public record GetMyThreadsQuery(
    Guid UserId,
    UserRole Role,
    string? Filter,
    string? Search,
    int Page,
    int PageSize) : IRequest<GetMyThreadsResult>;

public record GetMyThreadsResult(
    bool Success,
    ThreadSummaryDto[] Threads = null!,
    int TotalCount = 0,
    string? Error = null,
    ErrorKind? Kind = null);

public record ThreadSummaryDto(
    Guid ThreadId,
    string OtherParticipantName,
    string? OtherParticipantSpecialty,
    string Subject,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    ThreadStatus Status,
    AppointmentStatus AppointmentStatus,
    Guid AppointmentId);
