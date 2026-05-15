using Halen.Domain.Enums;

namespace Halen.Application.Events;

public record KycReviewedEvent(
    Guid DoctorProfileId,
    Guid DoctorUserId,
    KycDecision Decision,
    string? RejectionReason,
    string AdminName);
