using Halen.Domain.Entities;

namespace Halen.Domain.Interfaces;

public interface ITenantScoped
{
    Guid ClinicId { get; set; }
    Clinic? Clinic { get; set; }
}
