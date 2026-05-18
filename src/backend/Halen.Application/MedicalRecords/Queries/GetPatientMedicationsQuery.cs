using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientMedicationsQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId
) : IRequest<GetPatientMedicationsResult>;

public record GetPatientMedicationsResult(
    bool Success,
    MedicationDto[] Medications = default!,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public MedicationDto[] Medications { get; init; } = Medications ?? [];
}

public record MedicationDto(
    Guid Id,
    string MedicationName,
    string Dosage,
    string Frequency,
    string? StartDate,
    string? EndDate,
    bool IsActive,
    string? PrescribedByName,
    Guid? LinkedPrescriptionId,
    string AddedBy,
    DateTime CreatedAt);
