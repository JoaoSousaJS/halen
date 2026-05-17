using FluentAssertions;
using Halen.Application.Doctors.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Doctors;

[TestClass]
public class SearchDoctorsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private SearchDoctorsQueryHandler _handler = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new SearchDoctorsQueryHandler(_db);
        await Task.CompletedTask;
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    #region Helpers

    private (User user, DoctorProfile profile) SeedDoctor(
        string firstName,
        string lastName,
        string specialty,
        decimal fee,
        int yearsOfExperience,
        KycStatus kycStatus = KycStatus.Approved,
        AccountStatus accountStatus = AccountStatus.Active,
        string? email = null)
    {
        var emailAddress = email ?? $"{firstName.ToLower()}.{lastName.ToLower()}@test.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = emailAddress,
            UserName = emailAddress,
            Role = UserRole.Doctor,
            Status = accountStatus,
        };
        _db.Users.Add(user);

        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Specialty = specialty,
            LicenseNumber = $"LIC-{Guid.NewGuid().ToString()[..8]}",
            ConsultationFee = fee,
            YearsOfExperience = yearsOfExperience,
            KycStatus = kycStatus,
        };
        _db.DoctorProfiles.Add(profile);

        return (user, profile);
    }

    private void SeedAvailability(Guid doctorProfileId, DayOfWeek day, bool isActive = true)
    {
        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = day,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            SlotDurationMinutes = 20,
            IsActive = isActive,
        });
    }

    private static SearchDoctorsQuery Query(
        string? searchTerm = null,
        string? specialty = null,
        decimal? minFee = null,
        decimal? maxFee = null,
        DayOfWeek? availableOn = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 20)
        => new(searchTerm, specialty, minFee, maxFee, availableOn, sortBy, page, pageSize);

    #endregion

    [TestMethod]
    public async Task Handle_FiltersOnlyApprovedActiveDoctors()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 150, 10); // approved + active
        SeedDoctor("Bob", "Lima", "Neurology", 200, 5, KycStatus.NotSubmitted); // unapproved
        SeedDoctor("Carlos", "Dias", "Dermatology", 180, 8, KycStatus.Approved, AccountStatus.Suspended); // inactive
        SeedDoctor("Diana", "Reis", "Pediatrics", 120, 3, KycStatus.Submitted); // pending KYC
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Doctors.Should().HaveCount(1);
        result.Doctors[0].Name.Should().Be("Ana Costa");
    }

    [TestMethod]
    public async Task Handle_FuzzySearchOnName_MatchesCaseInsensitive()
    {
        SeedDoctor("Dr", "Silva", "Cardiology", 150, 10);
        SeedDoctor("Maria", "Santos", "Dermatology", 180, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(searchTerm: "silva"), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Doctors[0].Name.Should().Be("Dr Silva");
    }

    [TestMethod]
    public async Task Handle_FuzzySearchOnSpecialty_MatchesPartial()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 150, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 200, 5);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(searchTerm: "cardio"), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Doctors[0].Specialty.Should().Be("Cardiology");
    }

    [TestMethod]
    public async Task Handle_ExactSpecialtyFilter_ReturnsOnlyMatchingSpecialty()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 150, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 200, 5);
        SeedDoctor("Carlos", "Dias", "Cardiology", 180, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(specialty: "Cardiology"), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Doctors.Should().AllSatisfy(d => d.Specialty.Should().Be("Cardiology"));
    }

    [TestMethod]
    public async Task Handle_FeeRangeMinOnly_ReturnsDoctorsAboveMinFee()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 80, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 150, 5);
        SeedDoctor("Carlos", "Dias", "Neurology", 200, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(minFee: 100), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Doctors.Should().AllSatisfy(d => d.ConsultationFee.Should().BeGreaterThanOrEqualTo(100));
    }

    [TestMethod]
    public async Task Handle_FeeRangeMaxOnly_ReturnsDoctorsBelowMaxFee()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 80, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 150, 5);
        SeedDoctor("Carlos", "Dias", "Neurology", 250, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(maxFee: 200), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Doctors.Should().AllSatisfy(d => d.ConsultationFee.Should().BeLessThanOrEqualTo(200));
    }

    [TestMethod]
    public async Task Handle_FeeRangeBoth_ReturnsDoctorsWithinRange()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 80, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 150, 5);
        SeedDoctor("Carlos", "Dias", "Neurology", 250, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(minFee: 100, maxFee: 200), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Doctors[0].ConsultationFee.Should().Be(150);
    }

    [TestMethod]
    public async Task Handle_AvailabilityDayFilter_ReturnsOnlyDoctorsAvailableOnThatDay()
    {
        var (_, profile1) = SeedDoctor("Ana", "Costa", "Cardiology", 150, 10);
        var (_, profile2) = SeedDoctor("Bob", "Lima", "Dermatology", 200, 5);
        SeedAvailability(profile1.Id, DayOfWeek.Monday);
        SeedAvailability(profile2.Id, DayOfWeek.Wednesday);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(availableOn: DayOfWeek.Monday), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Doctors[0].Name.Should().Be("Ana Costa");
    }

    [TestMethod]
    public async Task Handle_AvailabilityDayFilter_ExcludesInactiveAvailabilities()
    {
        var (_, profile1) = SeedDoctor("Ana", "Costa", "Cardiology", 150, 10);
        SeedAvailability(profile1.Id, DayOfWeek.Monday, isActive: false);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(availableOn: DayOfWeek.Monday), CancellationToken.None);

        result.TotalCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_SortByFeeAscending_ReturnsDoctorsInAscendingFeeOrder()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 250, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 100, 5);
        SeedDoctor("Carlos", "Dias", "Neurology", 180, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(sortBy: "fee_asc"), CancellationToken.None);

        result.Doctors.Select(d => d.ConsultationFee)
            .Should().BeInAscendingOrder();
    }

    [TestMethod]
    public async Task Handle_SortByFeeDescending_ReturnsDoctorsInDescendingFeeOrder()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 250, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 100, 5);
        SeedDoctor("Carlos", "Dias", "Neurology", 180, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(sortBy: "fee_desc"), CancellationToken.None);

        result.Doctors.Select(d => d.ConsultationFee)
            .Should().BeInDescendingOrder();
    }

    [TestMethod]
    public async Task Handle_SortByExperience_ReturnsDoctorsInDescendingExperienceOrder()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 150, 5);
        SeedDoctor("Bob", "Lima", "Dermatology", 200, 15);
        SeedDoctor("Carlos", "Dias", "Neurology", 180, 10);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(sortBy: "experience"), CancellationToken.None);

        result.Doctors.Select(d => d.YearsOfExperience)
            .Should().BeInDescendingOrder();
    }

    [TestMethod]
    public async Task Handle_Pagination_ReturnsCorrectPageWithTotalCount()
    {
        SeedDoctor("Ana", "Costa", "Cardiology", 150, 10);
        SeedDoctor("Bob", "Lima", "Dermatology", 200, 5);
        SeedDoctor("Carlos", "Dias", "Neurology", 180, 8);
        SeedDoctor("Diana", "Reis", "Pediatrics", 120, 3);
        SeedDoctor("Eduardo", "Alves", "Orthopedics", 220, 12);
        await _db.SaveChangesAsync();

        // Page 1
        var page1 = await _handler.Handle(Query(page: 1, pageSize: 2), CancellationToken.None);
        page1.TotalCount.Should().Be(5);
        page1.Doctors.Should().HaveCount(2);

        // Page 3 (last page with 1 remaining)
        var page3 = await _handler.Handle(Query(page: 3, pageSize: 2), CancellationToken.None);
        page3.TotalCount.Should().Be(5);
        page3.Doctors.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Handle_EmptyResults_ReturnsEmptyListWithZeroTotalCount()
    {
        // No doctors seeded at all
        var result = await _handler.Handle(Query(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Doctors.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_DefaultSort_SortsByLastNameThenFirstName()
    {
        SeedDoctor("Zara", "Almeida", "Cardiology", 150, 10);
        SeedDoctor("Ana", "Souza", "Dermatology", 200, 5);
        SeedDoctor("Bruno", "Almeida", "Neurology", 180, 8);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(Query(), CancellationToken.None);

        result.Doctors.Select(d => d.Name)
            .Should().ContainInOrder("Bruno Almeida", "Zara Almeida", "Ana Souza");
    }
}
