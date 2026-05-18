using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateFamilyHistoryCommandValidator : AbstractValidator<UpdateFamilyHistoryCommand>
{
    public UpdateFamilyHistoryCommandValidator()
    {
        RuleFor(x => x.CallerUserId).NotEmpty();
        RuleFor(x => x.FamilyHistoryId).NotEmpty();
        RuleFor(x => x.ConditionName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AgeAtOnset).InclusiveBetween(0, 150).When(x => x.AgeAtOnset.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
