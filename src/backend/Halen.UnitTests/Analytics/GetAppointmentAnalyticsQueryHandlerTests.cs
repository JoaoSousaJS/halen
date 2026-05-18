using FluentAssertions;
using Halen.Application.Analytics.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Analytics;

[TestClass]
public class GetAppointmentAnalyticsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetAppointmentAnalyticsQueryHandler _handler = null!;

    private Guid _clinicId;
    private DoctorProfile _doctor1 = null!;
    private PatientProfile _patient1 = null!;
    private PatientProfile _patient2 = null!;
    private PatientProfile _patient3 = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetAppointmentAnalyticsQueryHandler(_db);

        _clinicId = TestTenantContext.DefaultClinicId;

        _db.Clinics.Add(new Clinic { Id = _clinicId, Name = "Lisbon Sul", Slug = "lisbon-sul" });

        _doctor1 = SeedDoctor("Ana", "Cardiology", _clinicId);
        _patient1 = SeedPatient("Carlos", _clinicId);
        _patient2 = SeedPatient("Diana", _clinicId);
        _patient3 = SeedPatient("Eva", _clinicId);

        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    // ── Empty DB ──

    [TestMethod]
    public async Task Handle_EmptyDatabase_ReturnsZeroKpis()
    {
        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.BookedKpi.Total.Should().Be(0);
        result.CompletedKpi.Total.Should().Be(0);
        result.CancelledKpi.Total.Should().Be(0);
        result.AvgLeadTimeKpi.Value.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_EmptyDatabase_Returns24HourItems()
    {
        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.ByHourOfDay.Should().HaveCount(24);
        result.ByHourOfDay.Select(h => h.Hour).Should().BeInAscendingOrder();
        result.ByHourOfDay.All(h => h.Count == 0).Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_EmptyDatabase_Returns7DayItems()
    {
        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.ByDayOfWeek.Should().HaveCount(7);
        result.ByDayOfWeek[0].Day.Should().Be("Mon");
        result.ByDayOfWeek[6].Day.Should().Be("Sun");
    }

    // ── KPIs ──

    [TestMethod]
    public async Task Handle_BookedKpi_CountsAppointmentsCreatedInPeriod()
    {
        var now = DateTime.UtcNow;

        // 3 appointments created within the last 30 days (CreatedAt defaults to UtcNow via BaseEntity)
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Scheduled);
        SeedAppointment(_patient3, _doctor1, now.AddDays(-1), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        // All 3 were created within the period, regardless of status
        result.BookedKpi.Total.Should().Be(3);
    }

    [TestMethod]
    public async Task Handle_CompletedKpi_CountsOnlyCompletedInPeriod()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed);
        SeedAppointment(_patient3, _doctor1, now.AddDays(-1), AppointmentStatus.Scheduled);
        SeedAppointment(_patient1, _doctor1, now.AddDays(-2), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.CompletedKpi.Total.Should().Be(2);
    }

    [TestMethod]
    public async Task Handle_CancelledKpi_CountsOnlyCancelledInPeriod()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Cancelled);
        SeedAppointment(_patient3, _doctor1, now.AddDays(-1), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.CancelledKpi.Total.Should().Be(2);
    }

    // ── Avg Lead Time ──

    [TestMethod]
    public async Task Handle_AvgLeadTime_CalculatesAverageCreatedToScheduledGap()
    {
        var now = DateTime.UtcNow;

        // Appointment 1: created 10 days before scheduled -> lead time = 10 days
        SeedAppointmentWithLeadTime(_patient1, _doctor1,
            createdAt: now.AddDays(-15), scheduledAt: now.AddDays(-5), AppointmentStatus.Completed);

        // Appointment 2: created 4 days before scheduled -> lead time = 4 days
        SeedAppointmentWithLeadTime(_patient2, _doctor1,
            createdAt: now.AddDays(-7), scheduledAt: now.AddDays(-3), AppointmentStatus.Completed);

        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        // Average: (10 + 4) / 2 = 7 days
        result.AvgLeadTimeKpi.Value.Should().BeApproximately(7m, 0.5m);
    }

    // ── Day of Week ──

    [TestMethod]
    public async Task Handle_ByDayOfWeek_Returns7ItemsLabeledMonToSun()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.ByDayOfWeek.Should().HaveCount(7);

        var labels = result.ByDayOfWeek.Select(d => d.Day).ToArray();
        labels.Should().Equal("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun");
    }

    [TestMethod]
    public async Task Handle_ByDayOfWeek_RatiosSumToOne()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-4), AppointmentStatus.Completed);
        SeedAppointment(_patient3, _doctor1, now.AddDays(-3), AppointmentStatus.Scheduled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        var totalRatio = result.ByDayOfWeek.Sum(d => d.Ratio);
        totalRatio.Should().BeApproximately(1m, 0.01m);
    }

    // ── Hour of Day ──

    [TestMethod]
    public async Task Handle_ByHourOfDay_AlwaysReturns24Items()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.ByHourOfDay.Should().HaveCount(24);
        result.ByHourOfDay.Select(h => h.Hour).Should().Equal(Enumerable.Range(0, 24));
    }

    [TestMethod]
    public async Task Handle_ByHourOfDay_CountsAppointmentsAtCorrectHour()
    {
        // Seed appointments at a specific hour
        var baseDate = DateTime.UtcNow.Date.AddDays(-5);
        var atTenAm = baseDate.AddHours(10);
        var atThreePm = baseDate.AddHours(15);

        SeedAppointment(_patient1, _doctor1, atTenAm, AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, atTenAm.AddMinutes(30), AppointmentStatus.Completed);
        SeedAppointment(_patient3, _doctor1, atThreePm, AppointmentStatus.Scheduled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.ByHourOfDay.First(h => h.Hour == 10).Count.Should().Be(2);
        result.ByHourOfDay.First(h => h.Hour == 15).Count.Should().Be(1);
        result.ByHourOfDay.First(h => h.Hour == 0).Count.Should().Be(0);
    }

    // ── Daily Series ──

    [TestMethod]
    public async Task Handle_DailySeries_HasCorrectDataPoints()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-5), AppointmentStatus.Scheduled);
        SeedAppointment(_patient3, _doctor1, now.AddDays(-3), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentAnalyticsQuery("30d"), CancellationToken.None);

        result.DailySeries.Labels.Should().NotBeEmpty();
        result.DailySeries.Current.Should().HaveCount(result.DailySeries.Labels.Length);
        result.DailySeries.Current.Sum().Should().Be(3);
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

    /// <summary>
    /// Seeds an appointment with an explicit CreatedAt to control lead time calculations.
    /// Uses reflection to override the init-only CreatedAt property from BaseEntity.
    /// </summary>
    private Appointment SeedAppointmentWithLeadTime(
        PatientProfile patient, DoctorProfile doctor,
        DateTime createdAt, DateTime scheduledAt, AppointmentStatus status)
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

        // CreatedAt is init-only, so use the object initializer wouldn't work after construction.
        // We need to set it via reflection or use a workaround.
        // Actually, init properties can be set during object initialization:
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt))!
            .SetValue(appointment, createdAt);

        _db.Appointments.Add(appointment);
        return appointment;
    }
}
