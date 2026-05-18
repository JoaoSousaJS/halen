using FluentValidation;

namespace Halen.Application.MedicalRecords.Commands;

public class GrantRecordAccessCommandValidator : AbstractValidator<GrantRecordAccessCommand>
{
    public GrantRecordAccessCommandValidator()
    {
        RuleFor(x => x.PatientProfileId).NotEmpty();
        RuleFor(x => x.GrantToUserId).NotEmpty();
        RuleFor(x => x.AccessLevel).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
