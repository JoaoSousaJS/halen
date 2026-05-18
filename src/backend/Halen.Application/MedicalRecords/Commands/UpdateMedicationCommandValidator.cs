using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class UpdateMedicationCommandValidator : AbstractValidator<UpdateMedicationCommand>
{
    public UpdateMedicationCommandValidator()
    {
        RuleFor(x => x.CallerUserId).NotEmpty();
        RuleFor(x => x.MedicationId).NotEmpty();
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
    }
}
