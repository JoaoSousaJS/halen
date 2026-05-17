using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Consultations.Commands;

public record JoinConsultationRoomCommand(
    Guid UserId,
    Guid AppointmentId,
    string Role
) : IRequest<JoinConsultationRoomResult>;

public record JoinConsultationRoomResult(
    bool Success,
    bool ConsultationStarted = false,
    string? RoomCode = null,
    DateTimeOffset? StartedAt = null,
    string? Error = null,
    ErrorKind? Kind = null
);
