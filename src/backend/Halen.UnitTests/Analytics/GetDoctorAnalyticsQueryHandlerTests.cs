using FluentAssertions;
using Halen.Application.Analytics.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Analytics;

[TestClass]
public class GetDoctorAnalyticsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetDoctorAnalyticsQueryHandler _handler = null!;

    private Guid _clinicId;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new GetDoctorAnalyticsQueryHandler(_db);

        _clinicId = TestTenantContext.DefaultClinicId;
        _db.Clinics.Add(new Clinic { Id = _clinicId, Name = "Lisbon Sul", Slug = "lisbon-sul" });
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_EmptyDatabase_ReturnsEmptyArrays()
    {
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorAnalyticsQuery("30d"), CancellationToken.None);

        result.Ranked.Should().BeEmpty();
        result.TopRated.Should().BeEmpty();
        result.NeedsAttention.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_Ranked_SortedByConsultsDescending()
    {
        var doctor1 = SeedDoctor("Ana", "Cardiology");
        var doctor2 = SeedDoctor("Bruno", "General");
        var patient = SeedPatient("Carlos");
        await _db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        // Doctor 1: 3 completed appointments
        SeedAppointment(patient, doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        SeedAppointment(patient, doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        SeedAppointment(patient, doctor1, now.AddDays(-1), AppointmentStatus.Completed, 80m);

        // Doctor 2: 1 completed appointment
        SeedAppointment(patient, doctor2, now.AddDays(-2), AppointmentStatus.Completed, 60m);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorAnalyticsQuery("30d"), CancellationToken.None);

        result.Ranked.Should().HaveCount(2);
        result.Ranked[0].Name.Should().Contain("Ana");
        result.Ranked[0].Consults.Should().Be(3);
        result.Ranked[1].Name.Should().Contain("Bruno");
        result.Ranked[1].Consults.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_Ranked_CompletionPercentage_Correct()
    {
        var doctor = SeedDoctor("Ana", "Cardiology");
        var patient = SeedPatient("Carlos");
        await _db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        // 3 completed, 1 cancelled → completion = 3 / (3+1) = 75%
        SeedAppointment(patient, doctor, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        SeedAppointment(patient, doctor, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        SeedAppointment(patient, doctor, now.AddDays(-1), AppointmentStatus.Completed, 80m);
        SeedAppointment(patient, doctor, now.AddDays(-2), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorAnalyticsQuery("30d"), CancellationToken.None);

        result.Ranked[0].CompletionPct.Should().Be(75m);
    }

    [TestMethod]
    public async Task Handle_TopRated_RequiresMinimum50Reviews()
    {
        var doctorHigh = SeedDoctor("Ana", "Cardiology", rating: 4.9m, reviewCount: 60);
        var doctorLow = SeedDoctor("Bruno", "General", rating: 4.8m, reviewCount: 30);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorAnalyticsQuery("30d"), CancellationToken.None);

        result.TopRated.Should().HaveCount(1);
        result.TopRated[0].Name.Should().Contain("Ana");
        result.TopRated[0].Rating.Should().Be(4.9m);
        result.TopRated[0].ReviewCount.Should().Be(60);
    }

    [TestMethod]
    public async Task Handle_NeedsAttention_LowCompletion_WarnsBelow85Pct()
    {
        var doctor = SeedDoctor("Ana", "Cardiology");
        var patient = SeedPatient("Carlos");
        await _db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        // 4 completed + 1 cancelled = 80% completion (below 85%), total >= 5
        SeedAppointment(patient, doctor, now.AddDays(-10), AppointmentStatus.Completed);
        SeedAppointment(patient, doctor, now.AddDays(-8), AppointmentStatus.Completed);
        SeedAppointment(patient, doctor, now.AddDays(-6), AppointmentStatus.Completed);
        SeedAppointment(patient, doctor, now.AddDays(-4), AppointmentStatus.Completed);
        SeedAppointment(patient, doctor, now.AddDays(-2), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorAnalyticsQuery("30d"), CancellationToken.None);

        result.NeedsAttention.Should().ContainSingle(a =>
            a.Name.Contains("Ana") && a.Severity == "warn");
    }

    [TestMethod]
    public async Task Handle_NeedsAttention_LowRating_DangerBelow3Point5()
    {
        var doctor = SeedDoctor("Bruno", "General", rating: 3.2m, reviewCount: 15);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDoctorAnalyticsQuery("30d"), CancellationToken.None);

        result.NeedsAttention.Should().ContainSingle(a =>
            a.Name.Contains("Bruno") && a.Severity == "danger");
    }

    // ── Seed helpers ──

    private DoctorProfile SeedDoctor(string name, string specialty,
        decimal? rating = null, int reviewCount = 0)
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
            AverageRating = rating,
            ReviewCount = reviewCount,
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
        DateTime scheduledAt, AppointmentStatus status,
        decimal? paymentAmount = null)
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

        if (paymentAmount.HasValue)
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ClinicId = doctor.ClinicId,
                AppointmentId = appointment.Id,
                PatientProfileId = patient.Id,
                Amount = paymentAmount.Value,
                Status = PaymentStatus.Captured,
                IdempotencyKey = Guid.NewGuid().ToString(),
                CapturedAt = scheduledAt,
            };
            _db.Payments.Add(payment);
        }

        return appointment;
    }
}
