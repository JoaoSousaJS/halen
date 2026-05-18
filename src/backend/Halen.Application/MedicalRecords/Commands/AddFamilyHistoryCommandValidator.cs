using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class AddFamilyHistoryCommandValidator : AbstractValidator<AddFamilyHistoryCommand>
{
    public AddFamilyHistoryCommandValidator()
    {
        RuleFor(x => x.Relationship).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ConditionName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AgeAtOnset)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(150)
            .When(x => x.AgeAtOnset.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
