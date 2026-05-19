using FluentValidation;

namespace Halen.Application.Clinics.Commands;

public class CreateClinicAdminCommandValidator : AbstractValidator<CreateClinicAdminCommand>
{
    public CreateClinicAdminCommandValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();

        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.TemporaryPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");
    }
}
