using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientVitalsHistoryQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    VitalType VitalType,
    int DaysBack = 90
) : IRequest<GetPatientVitalsHistoryResult>;

public record GetPatientVitalsHistoryResult(
    bool Success,
    VitalReadingDetailDto[] Readings = default!,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public VitalReadingDetailDto[] Readings { get; init; } = Readings ?? [];
}

public record VitalReadingDetailDto(
    Guid Id,
    decimal Value,
    decimal? SecondaryValue,
    string Unit,
    DateTime MeasuredAt,
    string Source,
    string? Notes,
    string AddedBy);
