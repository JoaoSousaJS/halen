using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Queries;

public class GetThreadMessagesQueryHandler(
    IAppDbContext db,
    ILogger<GetThreadMessagesQueryHandler> logger)
    : IRequestHandler<GetThreadMessagesQuery, GetThreadMessagesResult>
{
    public async Task<GetThreadMessagesResult> Handle(GetThreadMessagesQuery request, CancellationToken ct)
    {
        var thread = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.Id == request.ThreadId, ct);

        if (thread is null)
            return new GetThreadMessagesResult(false, Error: "Thread not found", Kind: ErrorKind.NotFound);

        if (thread.PatientUserId != request.UserId && thread.DoctorUserId != request.UserId)
            return new GetThreadMessagesResult(false, Error: "You are not a participant in this thread", Kind: ErrorKind.Forbidden);

        var query = db.ChatMessages
            .Include(m => m.SenderUser)
            .Include(m => m.Attachments)
            .Where(m => m.ThreadId == request.ThreadId);

        var totalCount = await query.CountAsync(ct);

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MessageDto(
                m.Id,
                m.SenderUser!.FirstName + " " + m.SenderUser.LastName,
                m.SenderUser.Role,
                m.SenderUserId,
                m.Content,
                m.MessageType,
                m.IsRead,
                m.ReadAt,
                m.CreatedAt,
                m.Attachments.Select(a => new AttachmentDto(
                    a.Id, a.FileName, a.ContentType, a.FileSizeBytes, a.AttachmentType
                )).ToArray()))
            .ToArrayAsync(ct);

        return new GetThreadMessagesResult(true, messages, totalCount);
    }
}
