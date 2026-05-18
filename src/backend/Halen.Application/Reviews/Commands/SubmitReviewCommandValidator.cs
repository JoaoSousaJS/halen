using FluentValidation;

namespace Halen.Application.Reviews.Commands;

public class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.PatientUserId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Title).NotEmpty().Length(3, 120);
        RuleFor(x => x.Body).MaximumLength(600);
        RuleFor(x => x.Tags).Must(tags => tags is null || tags.Length <= 6)
            .WithMessage("Maximum 6 tags allowed.");
        RuleForEach(x => x.Tags).Must(tag => ReviewConstants.AllowedTags.Contains(tag))
            .WithMessage("Invalid tag.");
    }
}
