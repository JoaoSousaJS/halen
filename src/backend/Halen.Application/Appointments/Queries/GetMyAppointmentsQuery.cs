using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Appointments.Queries;

public record GetMyAppointmentsQuery(
    Guid UserId,
    UserRole UserRole
) : IRequest<GetMyAppointmentsResult>;

public record GetMyAppointmentsResult(IReadOnlyList<AppointmentDto> Appointments);

public record AppointmentDto(
    Guid Id,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Reason,
    string Status,
    string? Notes,
    string DoctorName,
    string Specialty,
    decimal ConsultationFee,
    string PatientName,
    Guid PatientId
);
