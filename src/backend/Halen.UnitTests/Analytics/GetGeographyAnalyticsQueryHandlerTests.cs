using FluentAssertions;
using Halen.Application.Analytics.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Analytics;

[TestClass]
public class GetGeographyAnalyticsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetGeographyAnalyticsQueryHandler _handler = null!;

    private Guid _clinic1Id;
    private Guid _clinic2Id;
    private DoctorProfile _doctor1 = null!;
    private DoctorProfile _doctor2 = null!;
    private PatientProfile _patient1 = null!;
    private PatientProfile _patient2 = null!;
    private PatientProfile _patient3 = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetGeographyAnalyticsQueryHandler(_db);

        _clinic1Id = TestTenantContext.DefaultClinicId;
        _clinic2Id = Guid.NewGuid();

        _db.Clinics.Add(new Clinic { Id = _clinic1Id, Name = "Lisbon Sul", Slug = "lisbon-sul" });
        _db.Clinics.Add(new Clinic { Id = _clinic2Id, Name = "Porto Centro", Slug = "porto-centro" });

        _doctor1 = SeedDoctor("Ana", "Cardiology", _clinic1Id);
        _doctor2 = SeedDoctor("Bruno", "General", _clinic2Id);
        _patient1 = SeedPatient("Carlos", _clinic1Id);
        _patient2 = SeedPatient("Diana", _clinic1Id);
        _patient3 = SeedPatient("Eva", _clinic2Id);

        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_EmptyDatabase_ReturnsEmptyRegionsAndCohorts()
    {
        var result = await _handler.Handle(new GetGeographyAnalyticsQuery("30d"), CancellationToken.None);

        result.Regions.Should().BeEmpty();
        result.Retention.Cohorts.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_Regions_GroupedByClinicName()
    {
        var now = DateTime.UtcNow;

        // Clinic 1: 2 appointments
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed);

        // Clinic 2: 1 appointment
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetGeographyAnalyticsQuery("30d"), CancellationToken.None);

        result.Regions.Should().HaveCount(2);
        result.Regions.Should().Contain(r => r.Name == "Lisbon Sul" && r.Consults == 2);
        result.Regions.Should().Contain(r => r.Name == "Porto Centro" && r.Consults == 1);
    }

    [TestMethod]
    public async Task Handle_Regions_DeltaCalculation_VsPreviousPeriod()
    {
        var now = DateTime.UtcNow;

        // Current period (last 30 days): 3 appointments at clinic 1
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient1, _doctor1, now.AddDays(-10), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-15), AppointmentStatus.Completed);

        // Previous period (30-60 days ago): 2 appointments at clinic 1
        SeedAppointment(_patient1, _doctor1, now.AddDays(-35), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-40), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetGeographyAnalyticsQuery("30d"), CancellationToken.None);

        var lisbon = result.Regions.First(r => r.Name == "Lisbon Sul");
        // (3 - 2) / 2 * 100 = 50%
        lisbon.DeltaPct.Should().Be(50m);
    }

    [TestMethod]
    public async Task Handle_Regions_IsTop_OnlyForHighestClinic()
    {
        var now = DateTime.UtcNow;

        // Clinic 1: 3 appointments (top)
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient1, _doctor1, now.AddDays(-3), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-1), AppointmentStatus.Completed);

        // Clinic 2: 1 appointment
        SeedAppointment(_patient3, _doctor2, now.AddDays(-2), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetGeographyAnalyticsQuery("30d"), CancellationToken.None);

        var lisbon = result.Regions.First(r => r.Name == "Lisbon Sul");
        lisbon.IsTop.Should().BeTrue();

        var porto = result.Regions.First(r => r.Name == "Porto Centro");
        porto.IsTop.Should().BeFalse();
    }

    [TestMethod]
    public async Task Handle_CohortRetention_CorrectMatrix()
    {
        var now = DateTime.UtcNow;
        // Align to start of week (Monday)
        var daysFromMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
        var thisWeekMonday = now.AddDays(-daysFromMonday).Date;

        // Week 1 (2 weeks ago): 3 patients have their first-ever appointment
        var week1Start = thisWeekMonday.AddDays(-14);
        SeedAppointment(_patient1, _doctor1, week1Start.AddHours(10), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, week1Start.AddDays(1).AddHours(10), AppointmentStatus.Completed);
        SeedAppointment(_patient3, _doctor2, week1Start.AddDays(2).AddHours(10), AppointmentStatus.Completed);

        // Week 2 (1 week ago): 2 of those 3 patients return
        var week2Start = thisWeekMonday.AddDays(-7);
        SeedAppointment(_patient1, _doctor1, week2Start.AddHours(14), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, week2Start.AddDays(1).AddHours(14), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetGeographyAnalyticsQuery("30d"), CancellationToken.None);

        // The cohort for week1 should exist
        result.Retention.Cohorts.Should().NotBeEmpty();

        // Find the cohort that starts in week1
        // Week 0 = 100% (all 3 patients), offset 1 = 2/3 = 66.67%
        var week1Cohort = result.Retention.Cohorts
            .FirstOrDefault(c => c.Weeks.Length >= 2 && c.Weeks[0] == 100);

        week1Cohort.Should().NotBeNull();
        week1Cohort!.Weeks[0].Should().Be(100);
        week1Cohort.Weeks[1].Should().BeApproximately(66.67m, 0.1m);
    }

    // ── Seed helpers ──

    private DoctorProfile SeedDoctor(string name, string specialty, Guid clinicId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = $"Dr. {name}",
            LastName = "Test",
            Email = $"{name.ToLower()}@test.com",
            UserName = $"{name.ToLower()}@test.com",
            Role = UserRole.Doctor,
            ClinicId = clinicId,
        };
        _db.Users.Add(user);

        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClinicId = clinicId,
            Specialty = specialty,
            LicenseNumber = $"LIC-{name}",
            ConsultationFee = 80,
            YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(profile);
        return profile;
    }

    private PatientProfile SeedPatient(string name, Guid clinicId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = name,
            LastName = "Test",
            Email = $"{name.ToLower()}@test.com",
            UserName = $"{name.ToLower()}@test.com",
            Role = UserRole.Patient,
            ClinicId = clinicId,
        };
        _db.Users.Add(user);

        var profile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClinicId = clinicId,
            DateOfBirth = new DateOnly(1990, 1, 1),
            City = "Lisbon",
        };
        _db.PatientProfiles.Add(profile);
        return profile;
    }

    private Appointment SeedAppointment(
        PatientProfile patient, DoctorProfile doctor,
        DateTime scheduledAt, AppointmentStatus status)
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            ClinicId = doctor.ClinicId,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            Status = status,
            Reason = "Consult",
        };
        _db.Appointments.Add(appointment);
        return appointment;
    }
}
