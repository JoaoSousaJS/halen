using FluentValidation;
using Halen.Domain.Enums;

namespace Halen.Application.Reviews.Commands;

public class ModerateReviewCommandValidator : AbstractValidator<ModerateReviewCommand>
{
    public ModerateReviewCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.Decision).NotEqual(ReviewModerationStatus.Pending)
            .WithMessage("Cannot set moderation status back to Pending.");
    }
}
