using FluentValidation;

namespace Halen.Application.Reviews.Queries;

public class GetModerationQueueQueryValidator : AbstractValidator<GetModerationQueueQuery>
{
    private static readonly string[] ValidFilters = ["pending", "all"];

    public GetModerationQueueQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Filter).Must(f => ValidFilters.Contains(f))
            .WithMessage("Filter must be one of: pending, all.");
    }
}
