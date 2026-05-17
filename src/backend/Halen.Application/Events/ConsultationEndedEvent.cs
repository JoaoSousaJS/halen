namespace Halen.Application.Events;

public record ConsultationEndedEvent(
    Guid AppointmentId,
    Guid DoctorUserId,
    Guid PatientUserId,
    string DoctorName,
    DateTimeOffset EndedAt);
