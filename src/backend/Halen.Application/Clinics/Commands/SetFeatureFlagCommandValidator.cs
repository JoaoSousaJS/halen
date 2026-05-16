using FluentValidation;
using Halen.Domain.Constants;

namespace Halen.Application.Clinics.Commands;

public class SetFeatureFlagCommandValidator : AbstractValidator<SetFeatureFlagCommand>
{
    public SetFeatureFlagCommandValidator()
    {
        RuleFor(x => x.FeatureKey)
            .NotEmpty()
            .Must(key => FeatureKeys.All.Contains(key))
            .WithMessage($"FeatureKey must be one of: {string.Join(", ", FeatureKeys.All)}");
    }
}
