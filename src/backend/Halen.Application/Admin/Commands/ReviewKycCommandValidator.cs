using FluentValidation;
using Halen.Domain.Enums;

namespace Halen.Application.Admin.Commands;

public class ReviewKycCommandValidator : AbstractValidator<ReviewKycCommand>
{
    public ReviewKycCommandValidator()
    {
        RuleFor(x => x.DoctorProfileId)
            .NotEmpty().WithMessage("Doctor profile ID is required.");

        RuleFor(x => x.Decision)
            .IsInEnum().WithMessage("Invalid KYC decision.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Rejection reason is required when rejecting.")
            .MaximumLength(1000)
            .When(x => x.Decision == KycDecision.Rejected);
    }
}
