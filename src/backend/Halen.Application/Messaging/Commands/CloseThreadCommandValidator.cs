using FluentValidation;

namespace Halen.Application.Messaging.Commands;

public class CloseThreadCommandValidator : AbstractValidator<CloseThreadCommand>
{
    public CloseThreadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ThreadId).NotEmpty();
    }
}
