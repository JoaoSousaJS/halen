using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class ReviewHelpfulVote : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public Guid ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public Guid UserId { get; set; }
}
