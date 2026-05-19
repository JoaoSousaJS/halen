using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Appointments.Commands;

public record BookAppointmentCommand(
    Guid UserId,
    Guid DoctorId,
    DateTime ScheduledAt,
    string Reason
) : IRequest<BookAppointmentResult>, IAuditableCommand
{
    Guid IAuditableCommand.ActorId => UserId;
}


public record BookAppointmentResult(bool Success, Guid? AppointmentId, string? Error = null, string? PaymentStatus = null);
