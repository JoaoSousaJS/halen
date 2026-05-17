using FluentValidation;

namespace Halen.Application.Doctors.Queries;

public class SearchDoctorsQueryValidator : AbstractValidator<SearchDoctorsQuery>
{
    private static readonly string[] AllowedSortValues = ["name", "fee_asc", "fee_desc", "experience"];

    public SearchDoctorsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50);

        RuleFor(x => x.MinFee)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinFee.HasValue);

        RuleFor(x => x.MaxFee)
            .GreaterThanOrEqualTo(x => x.MinFee!.Value)
            .When(x => x.MinFee.HasValue && x.MaxFee.HasValue)
            .WithMessage("MaxFee must be greater than or equal to MinFee.");

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortValues.Contains(s))
            .When(x => x.SortBy is not null)
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortValues)}.");
    }
}
