using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record UpdateMedicationCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid MedicationId,
    string Dosage,
    string Frequency,
    DateOnly? EndDate,
    bool IsActive
) : IRequest<UpdateMedicationResult>;

public record UpdateMedicationResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);
