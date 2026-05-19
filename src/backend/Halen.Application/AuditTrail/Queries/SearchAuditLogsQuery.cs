using MediatR;

namespace Halen.Application.AuditTrail.Queries;

public record SearchAuditLogsQuery(
    Guid? ActorId,
    string? Action,
    string? TargetId,
    DateTime? From,
    DateTime? To,
    Guid? ClinicId,
    int Page = 1,
    int PageSize = 50
) : IRequest<SearchAuditLogsResult>;

public record SearchAuditLogsResult(IReadOnlyList<AuditLogDto> Logs, int TotalCount);

public record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    Guid ActorId,
    string ActorName,
    string Action,
    string TargetId,
    string? Metadata,
    string IpAddress
);
