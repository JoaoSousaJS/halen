using FluentValidation;

namespace Halen.Application.Messaging.Commands;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ThreadId).NotEmpty();
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(4000);
    }
}
