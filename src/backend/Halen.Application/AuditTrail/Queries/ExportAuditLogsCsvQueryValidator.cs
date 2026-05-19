using FluentValidation;
using Halen.Application.Interfaces;

namespace Halen.Application.AuditTrail.Queries;

public class ExportAuditLogsCsvQueryValidator : AbstractValidator<ExportAuditLogsCsvQuery>
{
    public ExportAuditLogsCsvQueryValidator(ITenantContext tenantContext)
    {
        RuleFor(q => q.From)
            .NotNull()
            .WithMessage("'From' date is required for CSV export.");

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
