using FluentValidation;

namespace Halen.Application.Reviews.Commands;

public class VoteHelpfulCommandValidator : AbstractValidator<VoteHelpfulCommand>
{
    public VoteHelpfulCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
