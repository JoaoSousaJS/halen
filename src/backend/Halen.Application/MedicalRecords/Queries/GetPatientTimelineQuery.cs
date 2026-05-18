using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.MedicalRecords.Queries;

public record GetPatientTimelineQuery(
    Guid CallerUserId,
    UserRole CallerRole,
    Guid PatientProfileId,
    string[]? FilterTypes,
    DateTime? From,
    DateTime? To,
    Guid? FilterDoctorId,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetPatientTimelineResult>;

public record GetPatientTimelineResult(
    bool Success,
    TimelineEntryDto[] Entries = default!,
    int TotalCount = 0,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public TimelineEntryDto[] Entries { get; init; } = Entries ?? [];
}

public record TimelineEntryDto(
    Guid Id,
    string Type,
    DateTime OccurredAt,
    string Title,
    string? Subtitle,
    string? AddedBy);
