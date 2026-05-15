using FluentValidation;

namespace Halen.Application.Prescriptions.Commands;

public class IssuePrescriptionCommandValidator : AbstractValidator<IssuePrescriptionCommand>
{
    public IssuePrescriptionCommandValidator()
    {
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RefillsRemaining).GreaterThanOrEqualTo(0).LessThanOrEqualTo(24);
        RuleFor(x => x.PharmacyName).MaximumLength(200);
    }
}
