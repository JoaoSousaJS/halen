using FluentValidation;

namespace Halen.Application.Prescriptions.Commands;

public class CancelPrescriptionCommandValidator : AbstractValidator<CancelPrescriptionCommand>
{
    public CancelPrescriptionCommandValidator()
    {
        RuleFor(x => x.DoctorUserId).NotEmpty();
        RuleFor(x => x.PrescriptionId).NotEmpty();
    }
}
