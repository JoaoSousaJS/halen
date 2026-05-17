namespace Halen.Application.Events;

public record PaymentRefundedEvent(Guid PaymentId, Guid AppointmentId, Guid PatientUserId, decimal Amount, string Currency);
