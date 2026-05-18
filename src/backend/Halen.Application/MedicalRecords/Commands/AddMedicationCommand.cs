using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record AddMedicationCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    string MedicationName,
    string Dosage,
    string Frequency,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? PrescribedByName,
    Guid? LinkedPrescriptionId
) : IRequest<AddMedicationResult>;

public record AddMedicationResult(
    bool Success,
    Guid? MedicationId = null,
    string? Error = null,
    ErrorKind? Kind = null);
