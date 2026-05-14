namespace Halen.Application.Events;

public record AppointmentCompletedEvent(
    Guid AppointmentId,
    Guid DoctorUserId,
    Guid PatientUserId,
    string DoctorName);
