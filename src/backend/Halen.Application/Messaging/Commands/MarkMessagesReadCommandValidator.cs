using FluentValidation;

namespace Halen.Application.Messaging.Commands;

public class MarkMessagesReadCommandValidator : AbstractValidator<MarkMessagesReadCommand>
{
    public MarkMessagesReadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ThreadId).NotEmpty();
    }
}
