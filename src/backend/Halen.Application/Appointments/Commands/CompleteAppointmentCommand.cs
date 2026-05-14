using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Appointments.Commands;

public record CompleteAppointmentCommand(
    Guid UserId,
    Guid AppointmentId,
    string? Notes
) : IRequest<CompleteAppointmentResult>;

public record CompleteAppointmentResult(bool Success, string? Error = null, ErrorKind? Kind = null);
