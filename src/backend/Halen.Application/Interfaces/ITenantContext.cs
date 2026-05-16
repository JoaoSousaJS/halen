namespace Halen.Application.Interfaces;

public interface ITenantContext
{
    Guid ClinicId { get; }
    bool IsPlatformAdmin { get; }
}
