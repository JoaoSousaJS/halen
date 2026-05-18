using MediatR;

namespace Halen.Application.Reviews.Queries;

public record GetDoctorReviewsQuery(
    Guid DoctorProfileId,
    int Page = 1,
    int PageSize = 10,
    string SortBy = "newest"
) : IRequest<DoctorReviewsResult>;

public record DoctorReviewsResult(
    IReadOnlyList<ReviewDto> Reviews,
    int TotalCount,
    decimal? AverageRating,
    int ReviewCount,
    IReadOnlyList<RatingBreakdownDto> RatingBreakdown,
    IReadOnlyList<TagCountDto> TopTags);

public record ReviewDto(
    Guid Id,
    int Rating,
    string Title,
    string Body,
    string[] Tags,
    string PostedAs,
    int HelpfulCount,
    string? DoctorResponse,
    DateTime? DoctorRespondedAt,
    DateTime CreatedAt);

public record RatingBreakdownDto(int Stars, int Count);

public record TagCountDto(string Tag, int Count);
