namespace Halen.Application.Events;

public record MedicalRecordUpdatedEvent(
    Guid PatientProfileId,
    Guid PatientUserId,
    string RecordType,
    string Action,
    Guid ActorUserId,
    DateTime OccurredAt);
