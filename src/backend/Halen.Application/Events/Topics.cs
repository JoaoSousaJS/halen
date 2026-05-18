namespace Halen.Application.Events;

public static class Topics
{
    public const string AppointmentBooked = "appointment.booked";
    public const string AppointmentCancelled = "appointment.cancelled";
    public const string AppointmentCompleted = "appointment.completed";
    public const string PrescriptionIssued = "prescription.issued";
    public const string PrescriptionCancelled = "prescription.cancelled";
    public const string KycSubmitted = "kyc.submitted";
    public const string KycReviewed = "kyc.reviewed";
    public const string PaymentCaptured = "payment.captured";
    public const string PaymentRefunded = "payment.refunded";
    public const string ConsultationEnded = "consultation.ended";
    public const string ReviewSubmitted = "review.submitted";
    public const string ReviewLowStar = "review.low_star";
}
