using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Consultations.Commands;

public record EndConsultationCommand(
    Guid UserId,
    Guid AppointmentId,
    string? Notes
) : IRequest<EndConsultationResult>;

public record EndConsultationResult(bool Success, DateTimeOffset? EndedAt = null, string? Error = null, ErrorKind? Kind = null);
