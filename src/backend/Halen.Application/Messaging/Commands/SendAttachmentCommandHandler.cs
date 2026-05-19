using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Commands;

public class SendAttachmentCommandHandler(
    IAppDbContext db,
    IFileStorage fileStorage,
    IEventBus eventBus,
    ILogger<SendAttachmentCommandHandler> logger)
    : IRequestHandler<SendAttachmentCommand, SendAttachmentResult>
{
    private const int RateLimitCount = 5;
    private const int RateLimitWindowSeconds = 60;

    public async Task<SendAttachmentResult> Handle(SendAttachmentCommand request, CancellationToken ct)
    {
        var thread = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.Id == request.ThreadId, ct);

        if (thread is null)
            return new SendAttachmentResult(false, Error: "Thread not found", Kind: ErrorKind.NotFound);

        if (thread.PatientUserId != request.UserId && thread.DoctorUserId != request.UserId)
            return new SendAttachmentResult(false, Error: "You are not a participant in this thread", Kind: ErrorKind.Forbidden);

        if (thread.Status == ThreadStatus.Closed)
            return new SendAttachmentResult(false, Error: "Thread is closed", Kind: ErrorKind.Validation);

        var cutoff = DateTime.UtcNow.AddSeconds(-RateLimitWindowSeconds);
        var recentCount = await db.ChatMessages
            .CountAsync(m => m.ThreadId == request.ThreadId
                          && m.SenderUserId == request.UserId
                          && m.MessageType == MessageType.Attachment
                          && m.CreatedAt > cutoff, ct);

        if (recentCount >= RateLimitCount)
            return new SendAttachmentResult(false, Error: "Attachment rate limit exceeded — please slow down", Kind: ErrorKind.Validation);

        if (!await VerifyMagicBytes(request.ContentType, request.FileStream))
            return new SendAttachmentResult(false, Error: "File content does not match declared type", Kind: ErrorKind.Validation);

        var message = new ChatMessage
        {
            ClinicId = thread.ClinicId,
            ThreadId = thread.Id,
            SenderUserId = request.UserId,
            MessageType = MessageType.Attachment,
            Content = $"📎 {Path.GetFileName(request.FileName)}",
        };
        db.ChatMessages.Add(message);

        var sanitizedName = SanitizeFileName(request.FileName);
        var storagePath = await fileStorage.SaveAsync(
            $"messaging/{thread.Id}/{message.Id}", sanitizedName, request.FileStream, ct);

        var attachmentType = request.ContentType.StartsWith("image/")
            ? MessageAttachmentType.Image
            : request.ContentType.StartsWith("audio/")
                ? MessageAttachmentType.VoiceMemo
                : MessageAttachmentType.Document;

        var attachment = new MessageAttachment
        {
            ClinicId = thread.ClinicId,
            MessageId = message.Id,
            FileName = Path.GetFileName(request.FileName),
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            StoragePath = storagePath,
            AttachmentType = attachmentType,
        };
        db.MessageAttachments.Add(attachment);

        thread.LastMessageAt = DateTimeOffset.UtcNow;
        thread.LastMessagePreview = message.Content;

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
            $"{sender.FirstName} {sender.LastName}", thread.LastMessagePreview!), ct);

        logger.LogInformation("Attachment {AttachmentId} sent in thread {ThreadId} by {UserId}",
            attachment.Id, thread.Id, request.UserId);

        return new SendAttachmentResult(true, message.Id);
    }

    private static async Task<bool> VerifyMagicBytes(string contentType, Stream stream)
    {
        if (!stream.CanSeek)
            return true;

        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, 4));
        stream.Position = 0;

        if (read < 4)
            return true;

        return contentType switch
        {
            "image/png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            "image/jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "application/pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            _ => true,
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9._\-]", "_");
    }
}
