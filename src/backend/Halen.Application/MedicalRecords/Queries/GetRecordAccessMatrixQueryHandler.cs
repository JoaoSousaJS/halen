using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.MedicalRecords.Queries;

public class GetRecordAccessMatrixQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetRecordAccessMatrixQuery, GetRecordAccessMatrixResult>
{
    public async Task<GetRecordAccessMatrixResult> Handle(GetRecordAccessMatrixQuery request, CancellationToken ct)
    {
        if (request.CallerRole != UserRole.PlatformAdmin)
            return new GetRecordAccessMatrixResult(false, Error: "Forbidden", Kind: ErrorKind.Forbidden);

        var query = db.RecordAccesses
            .AsNoTracking()
            .Where(ra => ra.PatientProfileId == request.PatientProfileId);

        var totalCount = await query.CountAsync(ct);

        var entries = await query
            .OrderByDescending(ra => ra.GrantedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ra => new
            {
                ra.Id,
                UserName = ra.GrantedToUser.FirstName + " " + ra.GrantedToUser.LastName,
                UserRole = ra.GrantedToUser.Role.ToString(),
                AccessLevel = ra.AccessLevel.ToString(),
                ra.GrantedAt,
                GrantedBy = ra.GrantedByUser.FirstName + " " + ra.GrantedByUser.LastName,
                ra.RevokedAt,
                ra.GrantedToUserId
            })
            .ToListAsync(ct);

        // For each entry, find the latest access log to get LastViewed
        var userIds = entries.Select(e => e.GrantedToUserId).Distinct().ToList();
        var latestLogs = await db.RecordAccessLogs
            .AsNoTracking()
            .Where(log => log.PatientProfileId == request.PatientProfileId
                          && userIds.Contains(log.AccessedByUserId))
            .GroupBy(log => log.AccessedByUserId)
            .Select(g => new { UserId = g.Key, LastViewed = g.Max(log => log.AccessedAt) })
            .ToDictionaryAsync(x => x.UserId, x => (DateTime?)x.LastViewed, ct);

        var result = entries.Select(e => new RecordAccessEntryDto(
            e.Id,
            e.UserName,
            e.UserRole,
            e.AccessLevel,
            e.GrantedAt,
            e.GrantedBy,
            e.RevokedAt,
            latestLogs.GetValueOrDefault(e.GrantedToUserId)
        )).ToArray();

        return new GetRecordAccessMatrixResult(true, result, totalCount);
    }
}
