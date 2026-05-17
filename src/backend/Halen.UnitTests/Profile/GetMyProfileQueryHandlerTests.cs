using FluentAssertions;
using Halen.Application.Profile.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Profile;

[TestClass]
public class GetMyProfileQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetMyProfileQueryHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetMyProfileQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_PatientUser_ReturnsPatientProfileFields()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            UserName = "john@test.com",
            Role = UserRole.Patient,
        };
        _db.Users.Add(user);

        var patientProfile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DateOfBirth = new DateOnly(1990, 5, 15),
            City = "New York",
            SubscriptionPlan = "Premium",
        };
        _db.PatientProfiles.Add(patientProfile);
        user.PatientProfile = patientProfile;
        await _db.SaveChangesAsync();

        var query = new GetMyProfileQuery(userId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Profile.Should().NotBeNull();
        result.Profile!.FirstName.Should().Be("John");
        result.Profile.LastName.Should().Be("Doe");
        result.Profile.Email.Should().Be("john@test.com");
        result.Profile.Role.Should().Be("Patient");
        result.Profile.DateOfBirth.Should().Be(new DateOnly(1990, 5, 15));
        result.Profile.City.Should().Be("New York");
        result.Profile.SubscriptionPlan.Should().Be("Premium");
        // Doctor-specific fields should be null for patient
        result.Profile.Specialty.Should().BeNull();
        result.Profile.ConsultationFee.Should().BeNull();
        result.Profile.YearsOfExperience.Should().BeNull();
        result.Profile.Languages.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_DoctorUser_ReturnsDoctorProfileFields()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "Dr",
            LastName = "House",
            Email = "house@test.com",
            UserName = "house@test.com",
            Role = UserRole.Doctor,
        };
        _db.Users.Add(user);

        var doctorProfile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Specialty = "Cardiology",
            LicenseNumber = "LIC-001",
            ConsultationFee = 200,
            YearsOfExperience = 15,
            Languages = new[] { "English", "Portuguese" },
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(doctorProfile);
        user.DoctorProfile = doctorProfile;
        await _db.SaveChangesAsync();

        var query = new GetMyProfileQuery(userId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Profile.Should().NotBeNull();
        result.Profile!.FirstName.Should().Be("Dr");
        result.Profile.LastName.Should().Be("House");
        result.Profile.Role.Should().Be("Doctor");
        result.Profile.Specialty.Should().Be("Cardiology");
        result.Profile.ConsultationFee.Should().Be(200);
        result.Profile.YearsOfExperience.Should().Be(15);
        result.Profile.Languages.Should().BeEquivalentTo(new[] { "English", "Portuguese" });
        // Patient-specific fields should be null for doctor
        result.Profile.DateOfBirth.Should().BeNull();
        result.Profile.City.Should().BeNull();
        result.Profile.SubscriptionPlan.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_AdminUser_ReturnsNoRoleSpecificFields()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@test.com",
            UserName = "admin@test.com",
            Role = UserRole.PlatformAdmin,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var query = new GetMyProfileQuery(userId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Profile.Should().NotBeNull();
        result.Profile!.FirstName.Should().Be("Admin");
        result.Profile.LastName.Should().Be("User");
        result.Profile.Role.Should().Be("PlatformAdmin");
        result.Profile.Specialty.Should().BeNull();
        result.Profile.ConsultationFee.Should().BeNull();
        result.Profile.YearsOfExperience.Should().BeNull();
        result.Profile.Languages.Should().BeNull();
        result.Profile.DateOfBirth.Should().BeNull();
        result.Profile.City.Should().BeNull();
        result.Profile.SubscriptionPlan.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_UserDoesNotExist_ReturnsNullProfile()
    {
        var query = new GetMyProfileQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Profile.Should().BeNull();
    }
}
