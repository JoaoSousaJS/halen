namespace Halen.Application.Events;

public record KycDocumentsSubmittedEvent(
    Guid DoctorProfileId,
    Guid DoctorUserId,
    string DoctorName);
