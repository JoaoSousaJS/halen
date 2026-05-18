using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetRecordAccessLogsQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetRecordAccessLogsResult>;

public record GetRecordAccessLogsResult(
    bool Success,
    RecordAccessLogDto[] Logs = default!,
    int TotalCount = 0,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public RecordAccessLogDto[] Logs { get; init; } = Logs ?? [];
}

public record RecordAccessLogDto(
    Guid Id,
    string AccessedBy,
    string Action,
    string ResourceType,
    DateTime AccessedAt);
