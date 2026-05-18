using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record UpdateFamilyHistoryCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid FamilyHistoryId,
    string ConditionName,
    int? AgeAtOnset,
    string? Notes
) : IRequest<UpdateFamilyHistoryResult>;

public record UpdateFamilyHistoryResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);
