using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Consultations.Commands;

public record StartConsultationCommand(
    Guid UserId,
    Guid AppointmentId
) : IRequest<StartConsultationResult>;

public record StartConsultationResult(bool Success, string? RoomCode = null, string? Error = null, ErrorKind? Kind = null);
