using Halen.Application.Interfaces;

namespace Halen.UnitTests.Helpers;

public class TestTenantContext : ITenantContext
{
    public static readonly Guid DefaultClinicId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid ClinicId { get; set; } = DefaultClinicId;
    public bool IsPlatformAdmin { get; set; } = true;
}
