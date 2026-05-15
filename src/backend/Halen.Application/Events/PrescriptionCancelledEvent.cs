namespace Halen.Application.Events;

public record PrescriptionCancelledEvent(
    Guid PrescriptionId,
    Guid DoctorUserId,
    Guid PatientUserId,
    string DrugName,
    string DoctorName,
    DateTime OccurredAt);
