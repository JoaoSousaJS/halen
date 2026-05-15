namespace Halen.Application.Events;

public record PrescriptionIssuedEvent(
    Guid PrescriptionId,
    Guid DoctorUserId,
    Guid PatientUserId,
    string DrugName,
    string DoctorName,
    DateTime OccurredAt);
