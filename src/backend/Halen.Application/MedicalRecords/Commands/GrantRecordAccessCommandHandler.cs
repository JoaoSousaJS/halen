using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.MedicalRecords.Commands;

public class GrantRecordAccessCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    ILogger<GrantRecordAccessCommandHandler> logger
) : IRequestHandler<GrantRecordAccessCommand, GrantRecordAccessResult>
{
    public async Task<GrantRecordAccessResult> Handle(
        GrantRecordAccessCommand request, CancellationToken ct)
    {
        // Only PlatformAdmin can grant record access
        var caller = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.CallerUserId, ct);

        if (caller is null || caller.Role != UserRole.PlatformAdmin)
            return new GrantRecordAccessResult(false,
                Error: "Only platform administrators can grant record access.",
                Kind: ErrorKind.Forbidden);

        var patient = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.Id == request.PatientProfileId, ct);

        if (patient is null)
            return new GrantRecordAccessResult(false,
                Error: "Patient not found.",
                Kind: ErrorKind.NotFound);

        var targetUser = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.GrantToUserId, ct);

        if (targetUser is null)
            return new GrantRecordAccessResult(false,
                Error: "User not found.",
                Kind: ErrorKind.NotFound);

        // Upsert: update existing or create new
        var existing = await db.RecordAccesses
            .FirstOrDefaultAsync(ra =>
                ra.PatientProfileId == request.PatientProfileId &&
                ra.GrantedToUserId == request.GrantToUserId, ct);

        if (existing is not null)
        {
            existing.AccessLevel = request.AccessLevel;
            existing.GrantedAt = DateTime.UtcNow;
            existing.GrantedByUserId = request.CallerUserId;
            existing.RevokedAt = null;
            existing.Reason = request.Reason;
            existing.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Record access {AccessId} updated for user {GrantToUserId} on patient {PatientProfileId}",
                existing.Id, request.GrantToUserId, request.PatientProfileId);

            return new GrantRecordAccessResult(true, existing.Id);
        }

        var access = new RecordAccess
        {
            ClinicId = tenantContext.ClinicId,
            PatientProfileId = request.PatientProfileId,
            GrantedToUserId = request.GrantToUserId,
            AccessLevel = request.AccessLevel,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = request.CallerUserId,
            Reason = request.Reason,
        };

        db.RecordAccesses.Add(access);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Record access {AccessId} granted to user {GrantToUserId} on patient {PatientProfileId}",
            access.Id, request.GrantToUserId, request.PatientProfileId);

        return new GrantRecordAccessResult(true, access.Id);
    }
}
