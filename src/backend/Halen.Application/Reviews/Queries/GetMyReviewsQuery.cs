using MediatR;

namespace Halen.Application.Reviews.Queries;

public record GetMyReviewsQuery(
    Guid DoctorUserId,
    int Page = 1,
    int PageSize = 10,
    string Filter = "all"
) : IRequest<GetMyReviewsResult>;

public record GetMyReviewsResult(
    IReadOnlyList<DoctorReviewItemDto> Reviews,
    int TotalCount,
    decimal? AverageRating,
    int ReviewCount);

public record DoctorReviewItemDto(
    Guid Id,
    int Rating,
    string Title,
    string Body,
    string[] Tags,
    string PostedAs,
    int HelpfulCount,
    string ModerationStatus,
    string? DoctorResponse,
    DateTime? DoctorRespondedAt,
    DateTime CreatedAt);
