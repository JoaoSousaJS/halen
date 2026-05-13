using MediatR;

namespace Halen.Application.Appointments.Commands;

public record BookAppointmentCommand(
    Guid UserId,
    Guid DoctorId,
    DateTime ScheduledAt,
    string Reason
) : IRequest<BookAppointmentResult>;

public record BookAppointmentResult(bool Success, Guid? AppointmentId, string? Error = null);
