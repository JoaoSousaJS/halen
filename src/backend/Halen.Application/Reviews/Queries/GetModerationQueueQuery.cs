using MediatR;

namespace Halen.Application.Reviews.Queries;

public record GetModerationQueueQuery(
    int Page = 1,
    int PageSize = 20,
    string Filter = "pending"
) : IRequest<ModerationQueueResult>;

public record ModerationQueueResult(
    IReadOnlyList<ModerationReviewDto> Reviews,
    int TotalCount);

public record ModerationReviewDto(
    Guid Id,
    int Rating,
    string Title,
    string Body,
    string[] Tags,
    string PostedAs,
    string ModerationStatus,
    string PatientName,
    string DoctorName,
    DateTime CreatedAt);
