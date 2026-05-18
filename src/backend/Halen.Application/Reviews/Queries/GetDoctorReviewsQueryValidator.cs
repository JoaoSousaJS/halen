using FluentValidation;

namespace Halen.Application.Reviews.Queries;

public class GetDoctorReviewsQueryValidator : AbstractValidator<GetDoctorReviewsQuery>
{
    private static readonly string[] ValidSortOptions = ["newest", "highest", "lowest", "helpful"];

    public GetDoctorReviewsQueryValidator()
    {
        RuleFor(x => x.DoctorProfileId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.SortBy).Must(s => ValidSortOptions.Contains(s))
            .WithMessage("SortBy must be one of: newest, highest, lowest, helpful.");
    }
}
