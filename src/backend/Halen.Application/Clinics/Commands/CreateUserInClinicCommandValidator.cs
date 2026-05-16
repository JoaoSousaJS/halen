using FluentValidation;
using Halen.Domain.Enums;

namespace Halen.Application.Clinics.Commands;

public class CreateUserInClinicCommandValidator : AbstractValidator<CreateUserInClinicCommand>
{
    public CreateUserInClinicCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TemporaryPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");

        RuleFor(x => x.Role)
            .Must(r => r is UserRole.Patient or UserRole.Doctor)
            .WithMessage("ClinicAdmin can only create Patient or Doctor users");
    }
}
