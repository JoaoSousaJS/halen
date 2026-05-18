using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record UpdateConditionCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid ConditionId,
    ConditionSeverity Severity,
    ConditionStatus Status,
    string? ClinicalNotes
) : IRequest<UpdateConditionResult>;

public record UpdateConditionResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);
