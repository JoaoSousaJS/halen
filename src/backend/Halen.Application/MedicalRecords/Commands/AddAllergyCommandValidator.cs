using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class AddAllergyCommandValidator : AbstractValidator<AddAllergyCommand>
{
    public AddAllergyCommandValidator()
    {
        RuleFor(x => x.AllergenName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reaction).MaximumLength(500);
        RuleFor(x => x.Severity).IsInEnum();
    }
}
