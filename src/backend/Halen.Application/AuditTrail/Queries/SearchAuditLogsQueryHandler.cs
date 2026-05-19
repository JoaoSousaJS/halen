using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.AuditTrail.Queries;

public class SearchAuditLogsQueryHandler(
    IAppDbContext db,
    ITenantContext tenantContext
) : IRequestHandler<SearchAuditLogsQuery, SearchAuditLogsResult>
{
    public async Task<SearchAuditLogsResult> Handle(SearchAuditLogsQuery request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking();

        if (request.ClinicId.HasValue && tenantContext.IsPlatformAdmin)
            query = query.IgnoreQueryFilters().Where(a => a.ClinicId == request.ClinicId.Value);

        if (request.ActorId.HasValue)
            query = query.Where(a => a.ActorId == request.ActorId.Value);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(a => a.Action == request.Action);

        if (!string.IsNullOrEmpty(request.TargetId))
            query = query.Where(a => a.TargetId == request.TargetId);

        if (request.From.HasValue)
            query = query.Where(a => a.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(a => a.CreatedAt <= request.To.Value);

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.CreatedAt,
                a.ActorId,
                a.ActorName,
                a.Action,
                a.TargetId,
                a.Metadata,
                a.IpAddress))
            .ToListAsync(ct);

        return new SearchAuditLogsResult(logs, totalCount);
    }
}
