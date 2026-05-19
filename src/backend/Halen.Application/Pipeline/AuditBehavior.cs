using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Pipeline;

public class AuditBehavior<TRequest, TResponse>(
    IAppDbContext db,
    ITenantContext tenantContext,
    IAuditContextProvider auditContext,
    ILogger<AuditBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IAuditableCommand auditable)
            return await next(ct);

        var response = await next(ct);

        try
        {
            var actorId = auditable.ActorId != Guid.Empty
                ? auditable.ActorId
                : auditContext.ActorId;

            var targetId = auditable.AuditTargetId
                          ?? TryExtractIdFromResult(response);

            var auditLog = new AuditLog
            {
                ClinicId = tenantContext.ClinicId,
                ActorId = actorId,
                ActorName = auditContext.ActorName,
                Action = auditable.AuditAction,
                TargetId = targetId ?? string.Empty,
                Metadata = AuditMetadataSerializer.Serialize(request),
                IpAddress = auditContext.IpAddress
            };

            db.AuditLogs.Add(auditLog);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write audit log for {Action}", auditable.AuditAction);
        }

        return response;
    }

    private static string? TryExtractIdFromResult(TResponse response)
    {
        if (response is null) return null;

        var type = response.GetType();
        var idProp = type.GetProperty("Id")
                    ?? type.GetProperty("AppointmentId")
                    ?? type.GetProperty("PrescriptionId")
                    ?? type.GetProperty("DoctorId")
                    ?? type.GetProperty("UserId");

        if (idProp?.GetValue(response) is Guid guid && guid != Guid.Empty)
            return guid.ToString();

        return null;
    }
}
