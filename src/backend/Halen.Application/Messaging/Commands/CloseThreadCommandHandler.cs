using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Commands;

public class CloseThreadCommandHandler(
    IAppDbContext db,
    IEventBus eventBus,
    ILogger<CloseThreadCommandHandler> logger)
    : IRequestHandler<CloseThreadCommand, CloseThreadResult>
{
    public async Task<CloseThreadResult> Handle(CloseThreadCommand request, CancellationToken ct)
    {
        var thread = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.Id == request.ThreadId, ct);

        if (thread is null)
            return new CloseThreadResult(false, Error: "Thread not found", Kind: ErrorKind.NotFound);

        if (thread.DoctorUserId != request.UserId)
            return new CloseThreadResult(false, Error: "Only the doctor can close a thread", Kind: ErrorKind.Forbidden);

        if (thread.Status == ThreadStatus.Closed)
            return new CloseThreadResult(false, Error: "Thread is already closed", Kind: ErrorKind.Validation);

        thread.Status = ThreadStatus.Closed;
        thread.ClosedAt = DateTimeOffset.UtcNow;
        thread.ClosedByUserId = request.UserId;

        var doctor = await db.Users.FirstAsync(u => u.Id == request.UserId, ct);

        var systemMessage = new ChatMessage
        {
            ClinicId = thread.ClinicId,
            ThreadId = thread.Id,
            SenderUserId = request.UserId,
            MessageType = MessageType.SystemEvent,
            Content = $"Thread closed by Dr. {doctor.LastName}",
        };
        db.ChatMessages.Add(systemMessage);

        thread.LastMessageAt = DateTimeOffset.UtcNow;
        thread.LastMessagePreview = systemMessage.Content;

        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(Topics.ThreadClosed, new ThreadClosedEvent(
            thread.Id, request.UserId, $"{doctor.FirstName} {doctor.LastName}"), ct);

        logger.LogInformation("Thread {ThreadId} closed by {UserId}", thread.Id, request.UserId);

        return new CloseThreadResult(true);
    }
}
