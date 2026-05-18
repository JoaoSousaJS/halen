using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetRecordAccessLogsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetRecordAccessLogsQuery, GetRecordAccessLogsResult>
{
    public async Task<GetRecordAccessLogsResult> Handle(GetRecordAccessLogsQuery request, CancellationToken ct)
    {
        if (request.CallerRole != UserRole.PlatformAdmin)
            return new GetRecordAccessLogsResult(false, Error: "Forbidden", Kind: ErrorKind.Forbidden);

        var query = db.RecordAccessLogs
            .AsNoTracking()
            .Where(log => log.PatientProfileId == request.PatientProfileId);

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(log => log.AccessedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(log => new RecordAccessLogDto(
                log.Id,
                log.AccessedByUser.FirstName + " " + log.AccessedByUser.LastName,
                log.Action,
                log.ResourceType,
                log.AccessedAt))
            .ToArrayAsync(ct);

        return new GetRecordAccessLogsResult(true, logs, totalCount);
    }
}
