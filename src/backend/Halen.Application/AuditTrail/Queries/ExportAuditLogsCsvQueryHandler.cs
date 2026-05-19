using System.Globalization;
using System.Text;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.AuditTrail.Queries;

public class ExportAuditLogsCsvQueryHandler(
    IAppDbContext db,
    ITenantContext tenantContext
) : IRequestHandler<ExportAuditLogsCsvQuery, ExportAuditLogsCsvResult>
{
    private const int MaxRows = 10_000;

    public async Task<ExportAuditLogsCsvResult> Handle(ExportAuditLogsCsvQuery request, CancellationToken ct)
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

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(MaxRows)
            .Select(a => new
            {
                a.CreatedAt,
                a.ActorName,
                a.Action,
                a.TargetId,
                a.IpAddress,
                a.Metadata
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,ActorName,Action,TargetId,IpAddress,Metadata");

        foreach (var log in logs)
        {
            sb.Append(EscapeCsv(log.CreatedAt.ToString("o", CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.Append(EscapeCsv(log.ActorName));
            sb.Append(',');
            sb.Append(EscapeCsv(log.Action));
            sb.Append(',');
            sb.Append(EscapeCsv(log.TargetId));
            sb.Append(',');
            sb.Append(EscapeCsv(log.IpAddress));
            sb.Append(',');
            sb.AppendLine(EscapeCsv(log.Metadata ?? string.Empty));
        }

        var fileName = $"audit-log-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return new ExportAuditLogsCsvResult(Encoding.UTF8.GetBytes(sb.ToString()), fileName);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        value = SanitizeFormulaInjection(value);

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    private static string SanitizeFormulaInjection(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@')
            return '\t' + value;

        return value;
    }
}
