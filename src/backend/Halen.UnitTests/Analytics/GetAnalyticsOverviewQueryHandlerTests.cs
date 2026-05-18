using FluentAssertions;
using Halen.Application.Analytics.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Analytics;

[TestClass]
public class GetAnalyticsOverviewQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetAnalyticsOverviewQueryHandler _handler = null!;

    private Guid _clinicId;
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
        _handler = new GetAnalyticsOverviewQueryHandler(_db);

        _clinicId = TestTenantContext.DefaultClinicId;
        _clinic2Id = Guid.NewGuid();

        _db.Clinics.Add(new Clinic { Id = _clinicId, Name = "Lisbon Sul", Slug = "lisbon-sul" });
        _db.Clinics.Add(new Clinic { Id = _clinic2Id, Name = "Porto Centro", Slug = "porto-centro" });

        _doctor1 = SeedDoctor("Ana", "Cardiology", _clinicId);
        _doctor2 = SeedDoctor("Bruno", "General", _clinic2Id);
        _patient1 = SeedPatient("Carlos", _clinicId);
        _patient2 = SeedPatient("Diana", _clinicId);
        _patient3 = SeedPatient("Eva", _clinic2Id);

        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_EmptyDatabase_ReturnsZeroKpis()
    {
        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.AppointmentKpi.Total.Should().Be(0);
        result.RevenueKpi.Value.Should().Be(0);
        result.ActiveUsersKpi.Total.Should().Be(0);
        result.NoShowKpi.Rate.Should().Be(0);
        result.Funnel.Should().NotBeNull();
        result.ClinicBreakdown.Should().BeEmpty();
        result.SpecialtyMix.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_WithAppointments_ReturnsCorrectKpis()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Scheduled, 60m);
        SeedAppointment(_patient1, _doctor1, now.AddDays(-2), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.AppointmentKpi.Total.Should().Be(3);
        result.ActiveUsersKpi.Total.Should().Be(3);
    }

    [TestMethod]
    public async Task Handle_NoShowRate_CalculatesCorrectly()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        // 1 cancelled / (2 completed + 1 cancelled) = 33.33%
        result.NoShowKpi.Rate.Should().BeApproximately(33.33m, 0.1m);
    }

    [TestMethod]
    public async Task Handle_Revenue_SumsCapturedPayments()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 120m, PaymentStatus.Captured);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Scheduled, 60m, PaymentStatus.Authorized);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.RevenueKpi.Value.Should().Be(200m);
    }

    [TestMethod]
    public async Task Handle_Delta_ComparesWithPreviousPeriod()
    {
        var now = DateTime.UtcNow;

        // Current period: 3 appointments
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Scheduled, 60m);

        // Previous period: 2 appointments
        SeedAppointment(_patient1, _doctor1, now.AddDays(-35), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-40), AppointmentStatus.Completed, 80m);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        // (3 - 2) / 2 * 100 = 50%
        result.AppointmentKpi.DeltaPct.Should().Be(50m);
    }

    [TestMethod]
    public async Task Handle_Sparkline_HasCorrectLength()
    {
        var now = DateTime.UtcNow;
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.AppointmentKpi.Sparkline.Should().HaveCount(30);
    }

    [TestMethod]
    public async Task Handle_Funnel_StagesInDescendingOrder()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Scheduled, 80m, PaymentStatus.Authorized);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.Funnel.Should().HaveCount(4);
        result.Funnel[0].Label.Should().Be("Booked");
        result.Funnel[0].Value.Should().BeGreaterThanOrEqualTo(result.Funnel[1].Value);
        result.Funnel[3].Label.Should().Be("Paid");
    }

    [TestMethod]
    public async Task Handle_ClinicBreakdown_GroupsCorrectly()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Scheduled, 60m);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.ClinicBreakdown.Should().HaveCount(2);
        result.ClinicBreakdown.Should().Contain(c => c.Name == "Lisbon Sul" && c.Value == 2);
        result.ClinicBreakdown.Should().Contain(c => c.Name == "Porto Centro" && c.Value == 1);
    }

    [TestMethod]
    public async Task Handle_SpecialtyMix_GroupsCorrectly()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Scheduled, 60m);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.SpecialtyMix.Should().HaveCount(2);
        result.SpecialtyMix.Should().Contain(s => s.Label == "Cardiology" && s.Value == 2);
        result.SpecialtyMix.Should().Contain(s => s.Label == "General" && s.Value == 1);
    }

    [TestMethod]
    public async Task Handle_ActiveUsers_DAU_WAU_MAU()
    {
        var now = DateTime.UtcNow;

        // Today: 1 patient
        SeedAppointment(_patient1, _doctor1, now.AddHours(-2), AppointmentStatus.Scheduled, 80m);
        // This week: 2 patients
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m);
        // This month: 3 patients
        SeedAppointment(_patient3, _doctor2, now.AddDays(-15), AppointmentStatus.Completed, 60m);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAnalyticsOverviewQuery("30d"), CancellationToken.None);

        result.ActiveUsers.Dau.Should().Be(1);
        result.ActiveUsers.Wau.Should().Be(2);
        result.ActiveUsers.Mau.Should().Be(3);
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
        DateTime scheduledAt, AppointmentStatus status,
        decimal? paymentAmount = null, PaymentStatus paymentStatus = PaymentStatus.Pending)
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
                Status = paymentStatus,
                IdempotencyKey = Guid.NewGuid().ToString(),
                CapturedAt = paymentStatus == PaymentStatus.Captured ? scheduledAt : null,
                RefundedAt = paymentStatus == PaymentStatus.Refunded ? scheduledAt : null,
            };
            _db.Payments.Add(payment);
        }

        return appointment;
    }
}
