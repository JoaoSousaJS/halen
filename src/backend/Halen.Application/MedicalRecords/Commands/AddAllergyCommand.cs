using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record AddAllergyCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    string AllergenName,
    string? Reaction,
    ConditionSeverity Severity,
    DateOnly? DateIdentified
) : IRequest<AddAllergyResult>;

public record AddAllergyResult(
    bool Success,
    Guid? AllergyId = null,
    string? Error = null,
    ErrorKind? Kind = null);
