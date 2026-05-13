using MediatR;

namespace Halen.Application.Appointments.Commands;

public record CancelAppointmentCommand(
    Guid UserId,
    string UserRole,
    Guid AppointmentId
) : IRequest<CancelAppointmentResult>;

public record CancelAppointmentResult(bool Success, string? Error = null);
