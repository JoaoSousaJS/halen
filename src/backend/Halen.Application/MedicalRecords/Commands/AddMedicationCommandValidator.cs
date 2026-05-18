using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class AddMedicationCommandValidator : AbstractValidator<AddMedicationCommand>
{
    public AddMedicationCommandValidator()
    {
        RuleFor(x => x.MedicationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrescribedByName).MaximumLength(200);
    }
}
