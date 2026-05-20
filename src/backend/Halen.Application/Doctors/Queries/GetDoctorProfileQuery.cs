using Halen.Application.Common;
using Halen.Application.Reviews.Queries;
using MediatR;

namespace Halen.Application.Doctors.Queries;

public record GetDoctorProfileQuery(
    Guid DoctorProfileId,
    int ReviewPage = 1,
    int ReviewPageSize = 10,
    string ReviewSortBy = "newest"
) : IRequest<GetDoctorProfileResult>;

public record GetDoctorProfileResult(
    bool Success,
    DoctorProfileDto? Doctor = null,
    IReadOnlyList<AvailabilityDayDto>? Availability = null,
    ReviewsSummaryDto? ReviewsSummary = null,
    IReadOnlyList<ReviewDto>? Reviews = null,
    int ReviewTotalCount = 0,
    string? Error = null,
    ErrorKind? Kind = null)
{
    public static GetDoctorProfileResult NotFound(string error) =>
        new(false, Error: error, Kind: ErrorKind.NotFound);

    public static GetDoctorProfileResult Ok(
        DoctorProfileDto doctor,
        IReadOnlyList<AvailabilityDayDto> availability,
        ReviewsSummaryDto? reviewsSummary,
        IReadOnlyList<ReviewDto> reviews,
        int reviewTotalCount) =>
        new(true, doctor, availability, reviewsSummary, reviews, reviewTotalCount);
}

public record DoctorProfileDto(
    Guid Id,
    string Name,
    string Specialty,
    decimal ConsultationFee,
    int YearsOfExperience,
    string[] Languages,
    decimal? AverageRating,
    int ReviewCount);

public record AvailabilityDayDto(
    string DayOfWeek,
    IReadOnlyList<TimeWindowDto> Windows);

public record TimeWindowDto(
    string StartTime,
    string EndTime,
    int SlotDurationMinutes);

public record ReviewsSummaryDto(
    decimal? AverageRating,
    int ReviewCount,
    IReadOnlyList<RatingBreakdownDto> RatingBreakdown,
    IReadOnlyList<TagCountDto> TopTags);
