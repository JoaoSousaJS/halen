using FluentAssertions;
using Halen.Application.Doctors.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Doctors;

[TestClass]
public class ListSpecialtiesQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private ListSpecialtiesQueryHandler _handler = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new ListSpecialtiesQueryHandler(_db);
        await Task.CompletedTask;
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    private void SeedDoctor(
        string specialty,
        KycStatus kycStatus = KycStatus.Approved,
        AccountStatus accountStatus = AccountStatus.Active)
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Dr",
            LastName = specialty.Replace(" ", ""),
            Email = email,
            UserName = email,
            Role = UserRole.Doctor,
            Status = accountStatus,
        };
        _db.Users.Add(user);

        _db.DoctorProfiles.Add(new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Specialty = specialty,
            LicenseNumber = $"LIC-{Guid.NewGuid().ToString()[..8]}",
            ConsultationFee = 100,
            YearsOfExperience = 5,
            KycStatus = kycStatus,
        });
    }

    [TestMethod]
    public async Task Handle_ReturnsDistinctSortedSpecialties()
    {
        SeedDoctor("Cardiology");
        SeedDoctor("Cardiology"); // duplicate
        SeedDoctor("Dermatology");
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new ListSpecialtiesQuery(), CancellationToken.None);

        result.Specialties.Should().BeEquivalentTo(
            new[] { "Cardiology", "Dermatology" },
            options => options.WithStrictOrdering());
    }

    [TestMethod]
    public async Task Handle_ExcludesNonApprovedDoctors()
    {
        SeedDoctor("Cardiology", KycStatus.Approved);
        SeedDoctor("Neurology", KycStatus.NotSubmitted);
        SeedDoctor("Pediatrics", KycStatus.Submitted);
        SeedDoctor("Orthopedics", KycStatus.Rejected);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new ListSpecialtiesQuery(), CancellationToken.None);

        result.Specialties.Should().ContainSingle()
            .Which.Should().Be("Cardiology");
    }

    [TestMethod]
    public async Task Handle_ExcludesInactiveUsers()
    {
        SeedDoctor("Cardiology", KycStatus.Approved, AccountStatus.Active);
        SeedDoctor("Dermatology", KycStatus.Approved, AccountStatus.Suspended);
        SeedDoctor("Neurology", KycStatus.Approved, AccountStatus.PendingReview);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new ListSpecialtiesQuery(), CancellationToken.None);

        result.Specialties.Should().ContainSingle()
            .Which.Should().Be("Cardiology");
    }

    [TestMethod]
    public async Task Handle_NoDoctors_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new ListSpecialtiesQuery(), CancellationToken.None);

        result.Specialties.Should().BeEmpty();
    }
}
