using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record AddFamilyHistoryCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    string Relationship,
    string ConditionName,
    int? AgeAtOnset,
    string? Notes
) : IRequest<AddFamilyHistoryResult>;

public record AddFamilyHistoryResult(
    bool Success,
    Guid? FamilyHistoryId = null,
    string? Error = null,
    ErrorKind? Kind = null);
