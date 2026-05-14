using FluentValidation;

namespace Halen.Application.Appointments.Commands;

public class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserRole).IsInEnum();
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}
