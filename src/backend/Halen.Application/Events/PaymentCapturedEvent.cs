namespace Halen.Application.Events;

public record PaymentCapturedEvent(Guid PaymentId, Guid AppointmentId, Guid PatientUserId, decimal Amount, string Currency);
