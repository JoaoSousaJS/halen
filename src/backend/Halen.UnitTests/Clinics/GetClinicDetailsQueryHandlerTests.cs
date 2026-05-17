using FluentAssertions;
using Halen.Application.Clinics.Queries;
using Halen.Application.Common;
using Halen.Domain.Entities;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Clinics;

[TestClass]
public class GetClinicDetailsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetClinicDetailsQueryHandler _handler = null!;
    private TestTenantContext _tenantContext = null!;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _tenantContext = new TestTenantContext { IsPlatformAdmin = true };
        _handler = new GetClinicDetailsQueryHandler(_db, _tenantContext);

        _clinicId = Guid.NewGuid();
        _db.Clinics.Add(new Clinic
        {
            Id = _clinicId, Name = "Test Clinic", Slug = "test-clinic", IsActive = true,
        });
        _db.ClinicFeatureFlags.AddRange(
            new ClinicFeatureFlag { ClinicId = _clinicId, FeatureKey = "video_calls", IsEnabled = true },
            new ClinicFeatureFlag { ClinicId = _clinicId, FeatureKey = "prescriptions", IsEnabled = false }
        );
        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ExistingClinic_ReturnsDetails()
    {
        var query = new GetClinicDetailsQuery(_clinicId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Clinic.Should().NotBeNull();
        result.Clinic!.Id.Should().Be(_clinicId);
        result.Clinic.Name.Should().Be("Test Clinic");
        result.Clinic.Slug.Should().Be("test-clinic");
        result.Clinic.IsActive.Should().BeTrue();
        result.Clinic.FeatureFlags.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Handle_NonExistentClinic_ReturnsNotFound()
    {
        var query = new GetClinicDetailsQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Should().Contain("not found");
    }
}
