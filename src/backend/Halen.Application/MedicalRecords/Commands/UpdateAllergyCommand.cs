using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record UpdateAllergyCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid AllergyId,
    string? Reaction,
    ConditionSeverity Severity,
    bool IsActive
) : IRequest<UpdateAllergyResult>;

public record UpdateAllergyResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);
