using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Appointments.Queries;

public record GetMyAppointmentsQuery(
    Guid UserId,
    UserRole UserRole,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetMyAppointmentsResult>;

public record GetMyAppointmentsResult(IReadOnlyList<AppointmentDto> Appointments, int TotalCount);

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
    Guid PatientId,
    string? PaymentStatus = null,
    decimal? PaymentAmount = null
);
