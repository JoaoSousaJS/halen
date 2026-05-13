using FluentValidation;

namespace Halen.Application.Auth.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        // No minimum length — the SignInManager rejects wrong passwords regardless of policy.
        // Enforcing strength here would block users registered under an older policy.
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
