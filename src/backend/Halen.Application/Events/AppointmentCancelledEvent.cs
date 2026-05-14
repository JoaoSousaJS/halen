namespace Halen.Application.Events;

public record AppointmentCancelledEvent(
    Guid AppointmentId,
    Guid CancelledByUserId,
    Guid PatientUserId,
    Guid DoctorUserId,
    string CancelledByName,
    string CancelledByRole);
