using FluentValidation;

namespace Halen.Application.Profile.Commands;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage("New password must be at least 8 characters");
        RuleFor(x => x.NewPassword).Matches("[A-Z]")
            .WithMessage("New password must contain at least one uppercase letter");
        RuleFor(x => x.NewPassword).Matches("[a-z]")
            .WithMessage("New password must contain at least one lowercase letter");
        RuleFor(x => x.NewPassword).Matches("[0-9]")
            .WithMessage("New password must contain at least one digit");
        RuleFor(x => x.NewPassword).NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from current password");
    }
}
