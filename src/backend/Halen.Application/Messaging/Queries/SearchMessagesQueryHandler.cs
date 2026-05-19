using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Queries;

public class SearchMessagesQueryHandler(
    IAppDbContext db,
    ILogger<SearchMessagesQueryHandler> logger)
    : IRequestHandler<SearchMessagesQuery, SearchMessagesResult>
{
    public async Task<SearchMessagesResult> Handle(SearchMessagesQuery request, CancellationToken ct)
    {
        var escaped = request.Query
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
        var searchPattern = $"%{escaped}%";

        var query = db.ChatMessages
            .Include(m => m.Thread)
            .ThenInclude(t => t!.PatientUser)
            .Include(m => m.Thread)
            .ThenInclude(t => t!.DoctorUser)
            .Include(m => m.SenderUser)
            .Include(m => m.Attachments)
            .Where(m => m.MessageType != MessageType.SystemEvent)
            .Where(m => m.Thread!.PatientUserId == request.UserId || m.Thread!.DoctorUserId == request.UserId)
            .Where(m => EF.Functions.ILike(m.Content, searchPattern));

        var totalCount = await query.CountAsync(ct);

        var hits = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new SearchHitDto(
                m.ThreadId,
                request.UserId == m.Thread!.PatientUserId
                    ? m.Thread.DoctorUser!.FirstName + " " + m.Thread.DoctorUser.LastName
                    : m.Thread.PatientUser!.FirstName + " " + m.Thread.PatientUser.LastName,
                m.Id,
                m.Content,
                m.SenderUser!.FirstName + " " + m.SenderUser.LastName,
                m.CreatedAt,
                m.Attachments.Any()))
            .ToArrayAsync(ct);

        return new SearchMessagesResult(true, hits, totalCount);
    }
}
