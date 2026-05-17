using FluentAssertions;
using Halen.Application.Clinics.Commands;
using Halen.Application.Common;
using Halen.Domain.Entities;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Halen.UnitTests.Clinics;

[TestClass]
public class SetFeatureFlagCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private SetFeatureFlagCommandHandler _handler = null!;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new SetFeatureFlagCommandHandler(_db);

        _clinicId = Guid.NewGuid();
        _db.Clinics.Add(new Clinic { Id = _clinicId, Name = "Test", Slug = "test" });
        await _db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Handle_NewFlag_CreatesEntry()
    {
        var command = new SetFeatureFlagCommand(_clinicId, "prescriptions", true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var flag = await _db.ClinicFeatureFlags
            .FirstOrDefaultAsync(f => f.ClinicId == _clinicId && f.FeatureKey == "prescriptions");
        flag.Should().NotBeNull();
        flag!.IsEnabled.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_ExistingFlag_UpdatesValue()
    {
        _db.ClinicFeatureFlags.Add(new ClinicFeatureFlag
        {
            ClinicId = _clinicId,
            FeatureKey = "kyc",
            IsEnabled = false,
        });
        await _db.SaveChangesAsync();

        var command = new SetFeatureFlagCommand(_clinicId, "kyc", true);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var flag = await _db.ClinicFeatureFlags
            .FirstAsync(f => f.ClinicId == _clinicId && f.FeatureKey == "kyc");
        flag.IsEnabled.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_NonExistentClinic_ReturnsNotFound()
    {
        var command = new SetFeatureFlagCommand(Guid.NewGuid(), "prescriptions", true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}
