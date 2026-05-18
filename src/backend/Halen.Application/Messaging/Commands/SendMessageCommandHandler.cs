using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Commands;

public class SendMessageCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<SendMessageCommandHandler> logger)
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private const int RateLimitCount = 10;
    private const int RateLimitWindowSeconds = 60;

    public async Task<SendMessageResult> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var thread = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.Id == request.ThreadId, ct);

        if (thread is null)
            return new SendMessageResult(false, Error: "Thread not found", Kind: ErrorKind.NotFound);

        if (thread.PatientUserId != request.UserId && thread.DoctorUserId != request.UserId)
            return new SendMessageResult(false, Error: "You are not a participant in this thread", Kind: ErrorKind.Forbidden);

        if (thread.Status == ThreadStatus.Closed)
            return new SendMessageResult(false, Error: "Thread is closed", Kind: ErrorKind.Validation);

        var cutoff = DateTime.UtcNow.AddSeconds(-RateLimitWindowSeconds);
        var recentCount = await db.ChatMessages
            .CountAsync(m => m.ThreadId == request.ThreadId
                          && m.SenderUserId == request.UserId
                          && m.CreatedAt > cutoff, ct);

        if (recentCount >= RateLimitCount)
            return new SendMessageResult(false, Error: "Rate limit exceeded — please slow down", Kind: ErrorKind.Validation);

        var content = request.Content.Trim();

        var message = new ChatMessage
        {
            ClinicId = thread.ClinicId,
            ThreadId = thread.Id,
            SenderUserId = request.UserId,
            MessageType = MessageType.Text,
            Content = content,
        };

        db.ChatMessages.Add(message);

        thread.LastMessageAt = DateTimeOffset.UtcNow;
        thread.LastMessagePreview = content.Length > 200 ? content[..200] : content;

        if (request.UserId == thread.PatientUserId)
            thread.DoctorUnreadCount++;
        else
            thread.PatientUnreadCount++;

        await db.SaveChangesAsync(ct);

        var recipientUserId = request.UserId == thread.PatientUserId
            ? thread.DoctorUserId
            : thread.PatientUserId;

        var sender = await db.Users.FirstAsync(u => u.Id == request.UserId, ct);

        await eventBus.PublishAsync(Topics.MessageSent, new MessageSentEvent(
            message.Id, thread.Id, request.UserId, recipientUserId,
            $"{sender.FirstName} {sender.LastName}", thread.LastMessagePreview), ct);

        logger.LogInformation("Message {MessageId} sent in thread {ThreadId} by {UserId}",
            message.Id, thread.Id, request.UserId);

        return new SendMessageResult(true, message.Id);
    }
}
