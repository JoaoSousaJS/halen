using FluentValidation;

namespace Halen.Application.Consultations.Commands;

public class JoinConsultationRoomCommandValidator : AbstractValidator<JoinConsultationRoomCommand>
{
    public JoinConsultationRoomCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
    }
}
