using Halen.Domain.Enums;

namespace Halen.Domain.Entities;

public class KycReview : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public Guid ReviewedByUserId { get; set; }
    public User ReviewedByUser { get; set; } = null!;

    public KycDecision Decision { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}
