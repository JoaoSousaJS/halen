using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class AddConditionCommandValidator : AbstractValidator<AddConditionCommand>
{
    public AddConditionCommandValidator()
    {
        RuleFor(x => x.CallerUserId).NotEmpty();
        RuleFor(x => x.PatientProfileId).NotEmpty();
        RuleFor(x => x.IcdCode)
            .NotEmpty()
            .MaximumLength(10)
            .Matches(@"^[A-Z]\d{2}(\.\d{1,4})?$")
            .WithMessage("ICD code must match format like A00 or A00.1234");
        RuleFor(x => x.IcdDescription).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.ClinicalNotes).MaximumLength(2000);
    }
}
