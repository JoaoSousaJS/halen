using FluentValidation;

namespace Halen.Application.Reviews.Queries;

public class GetMyReviewsQueryValidator : AbstractValidator<GetMyReviewsQuery>
{
    private static readonly string[] ValidFilters = ["all", "awaiting-reply", "low-star"];

    public GetMyReviewsQueryValidator()
    {
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Filter).Must(f => ValidFilters.Contains(f))
            .WithMessage("Filter must be one of: all, awaiting-reply, low-star.");
    }
}
