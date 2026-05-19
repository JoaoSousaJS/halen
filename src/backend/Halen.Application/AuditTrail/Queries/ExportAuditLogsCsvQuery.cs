using MediatR;

namespace Halen.Application.AuditTrail.Queries;

public record ExportAuditLogsCsvQuery(
    Guid? ActorId,
    string? Action,
    string? TargetId,
    DateTime? From,
    DateTime? To,
    Guid? ClinicId
) : IRequest<ExportAuditLogsCsvResult>;

public record ExportAuditLogsCsvResult(byte[] CsvBytes, string FileName);
