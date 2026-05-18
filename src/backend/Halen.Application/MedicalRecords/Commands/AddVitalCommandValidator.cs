using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class AddVitalCommandValidator : AbstractValidator<AddVitalCommand>
{
    public AddVitalCommandValidator()
    {
        RuleFor(x => x.VitalType).IsInEnum();
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.MeasuredAt)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("MeasuredAt cannot be more than 5 minutes in the future.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
