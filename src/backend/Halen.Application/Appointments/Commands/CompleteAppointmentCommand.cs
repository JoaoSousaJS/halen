using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Appointments.Commands;

public record CompleteAppointmentCommand(
    Guid UserId,
    Guid AppointmentId,
    string? Notes
) : IRequest<CompleteAppointmentResult>, IAuditableCommand
{
    Guid IAuditableCommand.ActorId => UserId;
    string? IAuditableCommand.AuditTargetId => AppointmentId.ToString();
}


public record CompleteAppointmentResult(bool Success, string? Error = null, ErrorKind? Kind = null);
