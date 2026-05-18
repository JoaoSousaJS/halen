using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class RevokeRecordAccessCommandHandler(
    IAppDbContext db,
    ILogger<RevokeRecordAccessCommandHandler> logger
) : IRequestHandler<RevokeRecordAccessCommand, RevokeRecordAccessResult>
{
    public async Task<RevokeRecordAccessResult> Handle(
        RevokeRecordAccessCommand request, CancellationToken ct)
    {
        // Only PlatformAdmin can revoke record access
        var caller = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.CallerUserId, ct);

        if (caller is null || caller.Role != UserRole.PlatformAdmin)
            return new RevokeRecordAccessResult(false,
                Error: "Only platform administrators can revoke record access.",
                Kind: ErrorKind.Forbidden);

        var access = await db.RecordAccesses
            .FirstOrDefaultAsync(ra => ra.Id == request.RecordAccessId, ct);

        if (access is null)
            return new RevokeRecordAccessResult(false,
                Error: "Record access not found.",
                Kind: ErrorKind.NotFound);

        access.AccessLevel = RecordAccessLevel.Revoked;
        access.RevokedAt = DateTime.UtcNow;
        access.Reason = request.Reason;
        access.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Record access {AccessId} revoked by {UserId}",
            request.RecordAccessId, request.CallerUserId);

        return new RevokeRecordAccessResult(true);
    }
}
