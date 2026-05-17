using FluentValidation;

namespace Halen.Application.Consultations.Queries;

public class GetConsultationRoomQueryValidator : AbstractValidator<GetConsultationRoomQuery>
{
    public GetConsultationRoomQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}
