using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetRecordAccessMatrixQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetRecordAccessMatrixResult>;

public record GetRecordAccessMatrixResult(
    bool Success,
    RecordAccessEntryDto[] Entries = default!,
    int TotalCount = 0,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public RecordAccessEntryDto[] Entries { get; init; } = Entries ?? [];
}

public record RecordAccessEntryDto(
    Guid Id,
    string UserName,
    string UserRole,
    string AccessLevel,
    DateTime GrantedAt,
    string GrantedBy,
    DateTime? RevokedAt,
    DateTime? LastViewed);
