using FluentAssertions;
using Halen.Application.Appointments.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Appointments;

[TestClass]
public class ListDoctorsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private ListDoctorsQueryHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new ListDoctorsQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ReturnsOnlyApprovedActiveDoctors()
    {
        var approvedActiveUser = CreateUser("Active", "Doc", UserRole.Doctor, AccountStatus.Active);
        var approvedActiveProfile = CreateDoctorProfile(approvedActiveUser.Id, KycStatus.Approved);

        var unapprovedUser = CreateUser("Unapproved", "Doc", UserRole.Doctor, AccountStatus.Active);
        CreateDoctorProfile(unapprovedUser.Id, KycStatus.NotSubmitted);

        var suspendedUser = CreateUser("Suspended", "Doc", UserRole.Doctor, AccountStatus.Suspended);
        CreateDoctorProfile(suspendedUser.Id, KycStatus.Approved);

        await _db.SaveChangesAsync();

        var query = new ListDoctorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Doctors.Should().HaveCount(1);
        result.Doctors[0].Id.Should().Be(approvedActiveProfile.Id);
        result.TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_ExcludesUnapprovedDoctors()
    {
        var activeUser = CreateUser("Active", "Doc", UserRole.Doctor, AccountStatus.Active);

        // Submitted but not yet approved
        CreateDoctorProfile(activeUser.Id, KycStatus.Submitted);
        await _db.SaveChangesAsync();

        var query = new ListDoctorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Doctors.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_ExcludesSuspendedDoctors()
    {
        var suspendedUser = CreateUser("Suspended", "Doc", UserRole.Doctor, AccountStatus.Suspended);
        CreateDoctorProfile(suspendedUser.Id, KycStatus.Approved);
        await _db.SaveChangesAsync();

        var query = new ListDoctorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Doctors.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Create 5 approved active doctors
        for (var i = 0; i < 5; i++)
        {
            var user = CreateUser($"Doctor{i}", $"Last{i:D2}", UserRole.Doctor, AccountStatus.Active);
            CreateDoctorProfile(user.Id, KycStatus.Approved);
        }
        await _db.SaveChangesAsync();

        var query = new ListDoctorsQuery(Page: 2, PageSize: 2);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Doctors.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Handle_EmptyList_ReturnsEmptyWithZeroCount()
    {
        var query = new ListDoctorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Doctors.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    private User CreateUser(string firstName, string lastName, UserRole role, AccountStatus status)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), FirstName = firstName, LastName = lastName,
            Email = $"{firstName.ToLower()}@test.com", UserName = $"{firstName.ToLower()}@test.com",
            Role = role, Status = status,
        };
        _db.Users.Add(user);
        return user;
    }

    private DoctorProfile CreateDoctorProfile(Guid userId, KycStatus kycStatus)
    {
        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(), UserId = userId,
            Specialty = "General", LicenseNumber = $"LIC-{Guid.NewGuid():N}",
            ConsultationFee = 100, YearsOfExperience = 5,
            KycStatus = kycStatus,
        };
        _db.DoctorProfiles.Add(profile);
        return profile;
    }
}
