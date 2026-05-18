using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record AddConditionCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    string IcdCode,
    string IcdDescription,
    DateOnly? DateOfOnset,
    ConditionSeverity Severity,
    ConditionStatus Status,
    string? ClinicalNotes,
    Guid? LinkedAppointmentId
) : IRequest<AddConditionResult>;

public record AddConditionResult(
    bool Success,
    Guid? ConditionId = null,
    string? Error = null,
    ErrorKind? Kind = null);
