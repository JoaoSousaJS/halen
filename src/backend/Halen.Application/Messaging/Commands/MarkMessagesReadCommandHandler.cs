using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Commands;

public class MarkMessagesReadCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<MarkMessagesReadCommandHandler> logger)
    : IRequestHandler<MarkMessagesReadCommand, MarkMessagesReadResult>
{
    public async Task<MarkMessagesReadResult> Handle(MarkMessagesReadCommand request, CancellationToken ct)
    {
        var thread = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.Id == request.ThreadId, ct);

        if (thread is null)
            return new MarkMessagesReadResult(false, Error: "Thread not found", Kind: ErrorKind.NotFound);

        if (thread.PatientUserId != request.UserId && thread.DoctorUserId != request.UserId)
            return new MarkMessagesReadResult(false, Error: "You are not a participant in this thread", Kind: ErrorKind.Forbidden);

        var now = DateTimeOffset.UtcNow;

        var unreadMessages = await db.ChatMessages
            .Where(m => m.ThreadId == request.ThreadId
                      && m.SenderUserId != request.UserId
                      && !m.IsRead)
            .ToListAsync(ct);

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = now;
        }

        if (request.UserId == thread.PatientUserId)
            thread.PatientUnreadCount = 0;
        else
            thread.DoctorUnreadCount = 0;

        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(Topics.MessagesRead, new MessagesReadEvent(
            thread.Id, request.UserId), ct);

        logger.LogInformation("Messages marked as read in thread {ThreadId} by {UserId}",
            thread.Id, request.UserId);

        return new MarkMessagesReadResult(true);
    }
}
