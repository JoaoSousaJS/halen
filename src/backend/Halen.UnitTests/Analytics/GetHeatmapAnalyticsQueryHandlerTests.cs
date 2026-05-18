using FluentAssertions;
using Halen.Application.Analytics.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Analytics;

[TestClass]
public class GetHeatmapAnalyticsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetHeatmapAnalyticsQueryHandler _handler = null!;

    private Guid _clinicId;
    private DoctorProfile _doctor1 = null!;
    private DoctorProfile _doctor2 = null!;
    private DoctorProfile _doctor3 = null!;
    private PatientProfile _patient1 = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetHeatmapAnalyticsQueryHandler(_db);

        _clinicId = TestTenantContext.DefaultClinicId;
        _db.Clinics.Add(new Clinic { Id = _clinicId, Name = "Lisbon Sul", Slug = "lisbon-sul" });

        _doctor1 = SeedDoctor("Ana", "Cardiology");
        _doctor2 = SeedDoctor("Bruno", "Dermatology");
        _doctor3 = SeedDoctor("Clara", "General");
        _patient1 = SeedPatient("Carlos");

        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    // ── Grid tests ──

    [TestMethod]
    public async Task Handle_Grid_AlwaysReturns7x24()
    {
        // No appointments at all — grid should still be 7 rows of 24 columns
        var result = await _handler.Handle(new GetHeatmapAnalyticsQuery("30d"), CancellationToken.None);

        result.Grid.Should().HaveCount(7);
        foreach (var row in result.Grid)
            row.Should().HaveCount(24);
    }

    [TestMethod]
    public async Task Handle_Grid_CorrectCellCount()
    {
        // Seed 2 appointments on the same day/hour so they land in the same cell
        // Use a known Wednesday at 14:00 UTC
        var wednesday14 = FindNextDayOfWeek(DateTime.UtcNow.AddDays(-10), DayOfWeek.Wednesday)
            .Date.AddHours(14);

        SeedAppointment(_patient1, _doctor1, wednesday14, AppointmentStatus.Completed);
        SeedAppointment(_patient1, _doctor1, wednesday14.AddMinutes(20), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetHeatmapAnalyticsQuery("30d"), CancellationToken.None);

        // Wednesday = .NET DayOfWeek 3 → remapped to row 2 (Mon=0)
        result.Grid[2][14].Should().Be(2);
    }

    [TestMethod]
    public async Task Handle_Grid_MondayIsRow0_SundayIsRow6()
    {
        // Seed an appointment on Monday at 09:00
        var monday9 = FindNextDayOfWeek(DateTime.UtcNow.AddDays(-10), DayOfWeek.Monday)
            .Date.AddHours(9);

        // Seed an appointment on Sunday at 18:00
        var sunday18 = FindNextDayOfWeek(DateTime.UtcNow.AddDays(-10), DayOfWeek.Sunday)
            .Date.AddHours(18);

        SeedAppointment(_patient1, _doctor1, monday9, AppointmentStatus.Completed);
        SeedAppointment(_patient1, _doctor1, sunday18, AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetHeatmapAnalyticsQuery("30d"), CancellationToken.None);

        // Monday → row 0, hour 9
        result.Grid[0][9].Should().Be(1);
        // Sunday → row 6, hour 18
        result.Grid[6][18].Should().Be(1);
    }

    // ── Specialty seasonality tests ──

    [TestMethod]
    public async Task Handle_SpecialtySeason_ReturnsTop3ByVolume()
    {
        var now = DateTime.UtcNow;

        // Cardiology: 5 appointments (most)
        for (var i = 0; i < 5; i++)
            SeedAppointment(_patient1, _doctor1, now.AddDays(-i - 1), AppointmentStatus.Completed);

        // Dermatology: 3 appointments
        for (var i = 0; i < 3; i++)
            SeedAppointment(_patient1, _doctor2, now.AddDays(-i - 1), AppointmentStatus.Completed);

        // General: 2 appointments
        for (var i = 0; i < 2; i++)
            SeedAppointment(_patient1, _doctor3, now.AddDays(-i - 1), AppointmentStatus.Completed);

        // Seed a 4th specialty doctor with 1 appointment — should be excluded from top 3
        var doctor4 = SeedDoctor("David", "Neurology");
        await _db.SaveChangesAsync();
        SeedAppointment(_patient1, doctor4, now.AddDays(-1), AppointmentStatus.Completed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetHeatmapAnalyticsQuery("30d"), CancellationToken.None);

        result.SpecialtySeries.Should().HaveCount(3);
        result.SpecialtySeries[0].Specialty.Should().Be("Cardiology");
        result.SpecialtySeries[1].Specialty.Should().Be("Dermatology");
        result.SpecialtySeries[2].Specialty.Should().Be("General");
    }

    // ── Average wait by specialty tests ──

    [TestMethod]
    public async Task Handle_AvgWaitBySpecialty_CalculatesCorrectly()
    {
        var now = DateTime.UtcNow;

        // Cardiology appointment: created 3 days before scheduled
        var appt1 = SeedAppointmentWithCreatedAt(
            _patient1, _doctor1,
            scheduledAt: now.AddDays(-1),
            createdAt: now.AddDays(-4),
            AppointmentStatus.Completed);

        // Another Cardiology appointment: created 5 days before scheduled
        var appt2 = SeedAppointmentWithCreatedAt(
            _patient1, _doctor1,
            scheduledAt: now.AddDays(-2),
            createdAt: now.AddDays(-7),
            AppointmentStatus.Completed);

        // Dermatology appointment: created 2 days before scheduled
        var appt3 = SeedAppointmentWithCreatedAt(
            _patient1, _doctor2,
            scheduledAt: now.AddDays(-1),
            createdAt: now.AddDays(-3),
            AppointmentStatus.Completed);

        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetHeatmapAnalyticsQuery("30d"), CancellationToken.None);

        // Cardiology avg wait = (3 + 5) / 2 = 4 days
        var cardiology = result.AvgWaitBySpecialty.First(w => w.Specialty == "Cardiology");
        cardiology.Days.Should().Be(4);

        // Dermatology avg wait = 2 days
        var dermatology = result.AvgWaitBySpecialty.First(w => w.Specialty == "Dermatology");
        dermatology.Days.Should().Be(2);
    }

    // ── Seed helpers ──

    private static DateTime FindNextDayOfWeek(DateTime from, DayOfWeek target)
    {
        var daysUntil = ((int)target - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(daysUntil);
    }

    private DoctorProfile SeedDoctor(string name, string specialty)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = $"Dr. {name}",
            LastName = "Test",
            Email = $"{name.ToLower()}@test.com",
            UserName = $"{name.ToLower()}@test.com",
            Role = UserRole.Doctor,
            ClinicId = _clinicId,
        };
        _db.Users.Add(user);

        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClinicId = _clinicId,
            Specialty = specialty,
            LicenseNumber = $"LIC-{name}",
            ConsultationFee = 80,
            YearsOfExperience = 5,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(profile);
        return profile;
    }

    private PatientProfile SeedPatient(string name)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = name,
            LastName = "Test",
            Email = $"{name.ToLower()}@test.com",
            UserName = $"{name.ToLower()}@test.com",
            Role = UserRole.Patient,
            ClinicId = _clinicId,
        };
        _db.Users.Add(user);

        var profile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClinicId = _clinicId,
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

    private Appointment SeedAppointmentWithCreatedAt(
        PatientProfile patient, DoctorProfile doctor,
        DateTime scheduledAt, DateTime createdAt,
        AppointmentStatus status)
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
            CreatedAt = createdAt,
        };
        _db.Appointments.Add(appointment);
        return appointment;
    }
}
