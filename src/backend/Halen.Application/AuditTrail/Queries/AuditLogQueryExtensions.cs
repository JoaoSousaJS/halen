using Halen.Application.Interfaces;
using Halen.Domain.Entities;

namespace Halen.Application.AuditTrail.Queries;

internal static class AuditLogQueryExtensions
{
    public static IQueryable<AuditLog> ApplyFilters(
        this IQueryable<AuditLog> query,
        Guid? actorId,
        string? action,
        string? targetId,
        DateTime? from,
        DateTime? to,
        Guid? clinicId,
        ITenantContext tenantContext)
    {
        if (clinicId.HasValue && tenantContext.IsPlatformAdmin)
            query = query.Where(a => a.ClinicId == clinicId.Value);

        if (actorId.HasValue)
            query = query.Where(a => a.ActorId == actorId.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(targetId))
            query = query.Where(a => a.TargetId == targetId);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return query;
    }
}
