using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Appointments.Commands;

public record CancelAppointmentCommand(
    Guid UserId,
    UserRole UserRole,
    Guid AppointmentId
) : IRequest<CancelAppointmentResult>;

public record CancelAppointmentResult(bool Success, string? Error = null, ErrorKind? Kind = null);
