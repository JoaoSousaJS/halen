using FluentValidation;

namespace Halen.Application.Clinics.Commands;

public class CreateClinicCommandValidator : AbstractValidator<CreateClinicCommand>
{
    public CreateClinicCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100)
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case (e.g. 'my-clinic')");
    }
}
