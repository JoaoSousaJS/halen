using FluentAssertions;
using Halen.Application.Clinics.Commands;
using Halen.Application.Common;
using Halen.Domain.Constants;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Halen.UnitTests.Clinics;

[TestClass]
public class CreateClinicCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private CreateClinicCommandHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new CreateClinicCommandHandler(_db);
    }

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesClinicWithFeatureFlags()
    {
        var command = new CreateClinicCommand("Test Clinic", "test-clinic");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ClinicId.Should().NotBeNull();

        var clinic = await _db.Clinics.FindAsync(result.ClinicId);
        clinic.Should().NotBeNull();
        clinic!.Name.Should().Be("Test Clinic");
        clinic.Slug.Should().Be("test-clinic");
        clinic.IsActive.Should().BeTrue();

        var flags = await _db.ClinicFeatureFlags
            .Where(f => f.ClinicId == result.ClinicId)
            .ToListAsync();
        flags.Should().HaveCount(FeatureKeys.All.Length);
        flags.Should().AllSatisfy(f => f.IsEnabled.Should().BeFalse());
    }

    [TestMethod]
    public async Task Handle_DuplicateSlug_ReturnsValidationError()
    {
        await _handler.Handle(new CreateClinicCommand("First", "same-slug"), CancellationToken.None);

        var result = await _handler.Handle(new CreateClinicCommand("Second", "same-slug"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("slug");
    }
}
