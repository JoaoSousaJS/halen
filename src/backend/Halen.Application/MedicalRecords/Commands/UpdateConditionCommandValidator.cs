using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateConditionCommandValidator : AbstractValidator<UpdateConditionCommand>
{
    public UpdateConditionCommandValidator()
    {
        RuleFor(x => x.CallerUserId).NotEmpty();
        RuleFor(x => x.ConditionId).NotEmpty();
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.ClinicalNotes).MaximumLength(2000);
    }
}
