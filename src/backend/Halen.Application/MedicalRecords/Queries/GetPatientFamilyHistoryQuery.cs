using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientFamilyHistoryQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId
) : IRequest<GetPatientFamilyHistoryResult>;

public record GetPatientFamilyHistoryResult(
    bool Success,
    FamilyHistoryDto[] Entries = default!,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public FamilyHistoryDto[] Entries { get; init; } = Entries ?? [];
}

public record FamilyHistoryDto(
    Guid Id,
    string Relationship,
    string ConditionName,
    int? AgeAtOnset,
    string? Notes,
    string AddedBy,
    DateTime CreatedAt);
