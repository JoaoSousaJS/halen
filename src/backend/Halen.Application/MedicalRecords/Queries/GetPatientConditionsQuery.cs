using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientConditionsQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId
) : IRequest<GetPatientConditionsResult>;

public record GetPatientConditionsResult(
    bool Success,
    ConditionDto[] Conditions = default!,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public ConditionDto[] Conditions { get; init; } = Conditions ?? [];
}

public record ConditionDto(
    Guid Id,
    string IcdCode,
    string IcdDescription,
    string? DateOfOnset,
    string Severity,
    string Status,
    string? ClinicalNotes,
    string AddedBy,
    DateTime CreatedAt);
