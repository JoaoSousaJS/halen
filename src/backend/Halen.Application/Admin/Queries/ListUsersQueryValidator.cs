using FluentValidation;

namespace Halen.Application.Admin.Queries;

public class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    private static readonly string[] AllowedRoles = ["patient", "doctor"];

    public ListUsersQueryValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => AllowedRoles.Contains(r!.ToLower()))
            .When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Role must be 'patient' or 'doctor'.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => x.Search is not null);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
