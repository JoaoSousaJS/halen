namespace Halen.Application.Events;

public record ReviewSubmittedEvent(
    Guid ReviewId,
    Guid DoctorUserId,
    Guid PatientUserId,
    int Rating,
    string PatientName,
    string DoctorName);
