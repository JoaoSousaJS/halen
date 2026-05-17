using FluentValidation;

namespace Halen.Application.Availability.Commands;

public class SetDoctorAvailabilityCommandValidator : AbstractValidator<SetDoctorAvailabilityCommand>
{
    public SetDoctorAvailabilityCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Slots).NotNull();
        RuleForEach(x => x.Slots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.DayOfWeek).IsInEnum();
            slot.RuleFor(s => s.StartTime).LessThan(s => s.EndTime)
                .WithMessage("StartTime must be before EndTime.");
        });
    }
}
