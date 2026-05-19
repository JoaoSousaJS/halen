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
        var query = db.AuditLogs.AsNoTracking()
            .ApplyFilters(request.ActorId, request.Action, request.TargetId,
                request.From, request.To, request.ClinicId, tenantContext);

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
