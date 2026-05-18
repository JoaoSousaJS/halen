using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientAllergiesQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId
) : IRequest<GetPatientAllergiesResult>;

public record GetPatientAllergiesResult(
    bool Success,
    AllergyDto[] Allergies = default!,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public AllergyDto[] Allergies { get; init; } = Allergies ?? [];
}

public record AllergyDto(
    Guid Id,
    string AllergenName,
    string? Reaction,
    string Severity,
    string? DateIdentified,
    bool IsActive,
    string AddedBy,
    DateTime CreatedAt);
