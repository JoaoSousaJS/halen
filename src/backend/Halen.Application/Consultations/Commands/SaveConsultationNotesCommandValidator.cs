using FluentValidation;

namespace Halen.Application.Consultations.Commands;

public class SaveConsultationNotesCommandValidator : AbstractValidator<SaveConsultationNotesCommand>
{
    public SaveConsultationNotesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(5000);
    }
}
