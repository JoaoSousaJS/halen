using FluentValidation;

namespace Halen.Application.Consultations.Commands;

public class StartConsultationCommandValidator : AbstractValidator<StartConsultationCommand>
{
    public StartConsultationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}
