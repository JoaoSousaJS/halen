using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class Payment : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public string? PaymentIntentId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public DateTime? CapturedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? FailureReason { get; set; }

    public void Authorize(string paymentIntentId)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot authorize payment in {Status} status.");
        Status = PaymentStatus.Authorized;
        PaymentIntentId = paymentIntentId;
    }

    public void Capture()
    {
        if (Status != PaymentStatus.Authorized)
            throw new InvalidOperationException($"Cannot capture payment in {Status} status.");
        Status = PaymentStatus.Captured;
        CapturedAt = DateTime.UtcNow;
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Authorized)
            throw new InvalidOperationException($"Cannot refund payment in {Status} status.");
        Status = PaymentStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
    }

    public void Fail(string? reason = null)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
    }
}
