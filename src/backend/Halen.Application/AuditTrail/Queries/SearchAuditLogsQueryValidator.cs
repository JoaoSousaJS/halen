using FluentValidation;
using Halen.Application.Interfaces;

namespace Halen.Application.AuditTrail.Queries;

public class SearchAuditLogsQueryValidator : AbstractValidator<SearchAuditLogsQuery>
{
    public SearchAuditLogsQueryValidator(ITenantContext tenantContext)
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);

        RuleFor(q => q.From)
            .LessThan(q => q.To)
            .When(q => q.From.HasValue && q.To.HasValue)
            .WithMessage("'From' must be before 'To'.");

        RuleFor(q => q.ClinicId)
            .Null()
            .When(_ => !tenantContext.IsPlatformAdmin)
            .WithMessage("Only PlatformAdmin can filter by ClinicId.");
    }
}
