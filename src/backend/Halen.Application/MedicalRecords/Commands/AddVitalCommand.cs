using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Commands;

public record AddVitalCommand(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    VitalType VitalType,
    decimal Value,
    decimal? SecondaryValue,
    string Unit,
    DateTime MeasuredAt,
    VitalSource Source,
    string? Notes
) : IRequest<AddVitalResult>;

public record AddVitalResult(
    bool Success,
    Guid? VitalId = null,
    string? Error = null,
    ErrorKind? Kind = null);
