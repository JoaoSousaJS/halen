using FluentValidation;

namespace Halen.Application.Reviews.Commands;

public class RespondToReviewCommandValidator : AbstractValidator<RespondToReviewCommand>
{
    public RespondToReviewCommandValidator()
    {
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Response).NotEmpty().Length(3, 600);
    }
}
