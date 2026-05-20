using FluentValidation;

namespace Halen.Application.Doctors.Queries;

public class GetDoctorProfileQueryValidator : AbstractValidator<GetDoctorProfileQuery>
{
    private static readonly string[] AllowedSortValues = ["newest", "highest", "lowest", "helpful"];

    public GetDoctorProfileQueryValidator()
    {
        RuleFor(x => x.DoctorProfileId).NotEmpty();
        RuleFor(x => x.ReviewPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ReviewPageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.ReviewSortBy).Must(s => AllowedSortValues.Contains(s))
            .WithMessage("ReviewSortBy must be one of: newest, highest, lowest, helpful.");
    }
}
