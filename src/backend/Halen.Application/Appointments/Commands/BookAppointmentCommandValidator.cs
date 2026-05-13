using FluentValidation;

namespace Halen.Application.Appointments.Commands;

public class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be in the future");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
