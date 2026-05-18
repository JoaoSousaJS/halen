namespace Halen.Application.Events;

public record ReviewLowStarEvent(
    Guid ReviewId,
    Guid DoctorUserId,
    Guid DoctorProfileId,
    int Rating,
    string PatientName,
    string DoctorName);
