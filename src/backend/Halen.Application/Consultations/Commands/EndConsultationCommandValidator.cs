using FluentValidation;

namespace Halen.Application.Consultations.Commands;

public class EndConsultationCommandValidator : AbstractValidator<EndConsultationCommand>
{
    public EndConsultationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(5000).When(x => x.Notes is not null);
    }
}
