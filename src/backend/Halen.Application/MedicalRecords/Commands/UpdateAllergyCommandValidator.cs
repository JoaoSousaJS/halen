using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateAllergyCommandValidator : AbstractValidator<UpdateAllergyCommand>
{
    public UpdateAllergyCommandValidator()
    {
        RuleFor(x => x.CallerUserId).NotEmpty();
        RuleFor(x => x.AllergyId).NotEmpty();
        RuleFor(x => x.Reaction).MaximumLength(500);
        RuleFor(x => x.Severity).IsInEnum();
    }
}
