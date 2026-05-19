using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Queries;

public class DownloadAttachmentQueryHandler(
    IAppDbContext db,
    IFileStorage fileStorage,
    ILogger<DownloadAttachmentQueryHandler> logger)
    : IRequestHandler<DownloadAttachmentQuery, DownloadAttachmentResult>
{
    public async Task<DownloadAttachmentResult> Handle(DownloadAttachmentQuery request, CancellationToken ct)
    {
        var attachment = await db.MessageAttachments
            .Include(a => a.Message)
            .ThenInclude(m => m!.Thread)
            .Where(a => a.Id == request.AttachmentId && a.Message!.ThreadId == request.ThreadId)
            .Select(a => new
            {
                a.StoragePath,
                a.ContentType,
                a.FileName,
                a.Message!.Thread!.PatientUserId,
                a.Message.Thread.DoctorUserId,
            })
            .FirstOrDefaultAsync(ct);

        if (attachment is null)
            return new DownloadAttachmentResult(false, Error: "Attachment not found", Kind: ErrorKind.NotFound);

        if (attachment.PatientUserId != request.UserId && attachment.DoctorUserId != request.UserId)
            return new DownloadAttachmentResult(false, Error: "You are not a participant in this thread", Kind: ErrorKind.Forbidden);

        var stream = await fileStorage.ReadAsync(attachment.StoragePath, ct);

        logger.LogInformation("Attachment {AttachmentId} downloaded by {UserId}", request.AttachmentId, request.UserId);

        return new DownloadAttachmentResult(true, stream, attachment.ContentType, attachment.FileName);
    }
}
