namespace Halen.Application.Events;

public record AppointmentBookedEvent(
    Guid AppointmentId,
    Guid PatientUserId,
    Guid DoctorUserId,
    DateTime ScheduledAt,
    string PatientName,
    string DoctorName);
