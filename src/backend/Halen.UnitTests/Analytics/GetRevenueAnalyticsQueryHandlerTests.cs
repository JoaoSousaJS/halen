using FluentAssertions;
using Halen.Application.Analytics.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Analytics;

[TestClass]
public class GetRevenueAnalyticsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetRevenueAnalyticsQueryHandler _handler = null!;

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
        _handler = new GetRevenueAnalyticsQueryHandler(_db);

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

    // ── Empty DB ──

    [TestMethod]
    public async Task Handle_EmptyDatabase_ReturnsZeroKpis()
    {
        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        result.GrossKpi.Value.Should().Be(0);
        result.NetKpi.Value.Should().Be(0);
        result.RefundsKpi.Value.Should().Be(0);
        result.ArpuKpi.Value.Should().Be(0);
        result.PaymentStatusBreakdown.Should().BeEmpty();
        result.ClinicRevenue.Should().BeEmpty();
    }

    // ── Gross Revenue ──

    [TestMethod]
    public async Task Handle_GrossKpi_SumsCapturedPayments()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 120m, PaymentStatus.Captured);
        // Authorized payment should NOT count
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Scheduled, 60m, PaymentStatus.Authorized);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        result.GrossKpi.Value.Should().Be(200m);
    }

    // ── Net Revenue ──

    [TestMethod]
    public async Task Handle_NetKpi_IsGrossMinusRefunds()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 100m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 80m, PaymentStatus.Captured);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-2), AppointmentStatus.Cancelled, 50m, PaymentStatus.Refunded);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        result.GrossKpi.Value.Should().Be(180m);
        result.RefundsKpi.Value.Should().Be(50m);
        result.NetKpi.Value.Should().Be(130m);
    }

    // ── ARPU ──

    [TestMethod]
    public async Task Handle_ArpuKpi_IsGrossDividedByDistinctPatients()
    {
        var now = DateTime.UtcNow;

        // Patient1: 2 captured payments (80 + 120 = 200)
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m, PaymentStatus.Captured);
        SeedAppointment(_patient1, _doctor1, now.AddDays(-4), AppointmentStatus.Completed, 120m, PaymentStatus.Captured);
        // Patient2: 1 captured payment (60)
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 60m, PaymentStatus.Captured);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        // Gross = 260, Distinct patients = 2 -> ARPU = 130
        result.ArpuKpi.Value.Should().Be(130m);
    }

    // ── Payment Status Breakdown ──

    [TestMethod]
    public async Task Handle_PaymentStatusBreakdown_PercentagesSumToApproximately100()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 100m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Scheduled, 80m, PaymentStatus.Authorized);
        SeedAppointment(_patient3, _doctor2, now.AddDays(-2), AppointmentStatus.Cancelled, 50m, PaymentStatus.Refunded);
        SeedAppointment(_patient1, _doctor1, now.AddDays(-1), AppointmentStatus.Scheduled, 70m, PaymentStatus.Pending);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        result.PaymentStatusBreakdown.Should().NotBeEmpty();
        var totalPct = result.PaymentStatusBreakdown.Sum(p => p.Percentage);
        totalPct.Should().BeApproximately(100m, 0.5m);
    }

    [TestMethod]
    public async Task Handle_PaymentStatusBreakdown_HasCorrectLabels()
    {
        var now = DateTime.UtcNow;

        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 100m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Scheduled, 80m, PaymentStatus.Authorized);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        result.PaymentStatusBreakdown.Select(p => p.Label)
            .Should().Contain("Captured")
            .And.Contain("Authorized");
    }

    // ── Clinic Revenue ──

    [TestMethod]
    public async Task Handle_ClinicRevenue_OrdersByRevenueDescending()
    {
        var now = DateTime.UtcNow;

        // Lisbon Sul: 80 + 120 = 200
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 120m, PaymentStatus.Captured);
        // Porto Centro: 60
        SeedAppointment(_patient3, _doctor2, now.AddDays(-1), AppointmentStatus.Completed, 60m, PaymentStatus.Captured);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        result.ClinicRevenue.Should().HaveCount(2);
        result.ClinicRevenue[0].Name.Should().Be("Lisbon Sul");
        result.ClinicRevenue[0].Revenue.Should().Be(200m);
        result.ClinicRevenue[1].Name.Should().Be("Porto Centro");
        result.ClinicRevenue[1].Revenue.Should().Be(60m);
    }

    [TestMethod]
    public async Task Handle_ClinicRevenue_CalculatesConsultsAndArpu()
    {
        var now = DateTime.UtcNow;

        // Lisbon Sul: 2 consults, 200 total -> ARPU = 100
        SeedAppointment(_patient1, _doctor1, now.AddDays(-5), AppointmentStatus.Completed, 80m, PaymentStatus.Captured);
        SeedAppointment(_patient2, _doctor1, now.AddDays(-3), AppointmentStatus.Completed, 120m, PaymentStatus.Captured);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRevenueAnalyticsQuery("30d"), CancellationToken.None);

        result.ClinicRevenue.Should().HaveCount(1);
        result.ClinicRevenue[0].Consults.Should().Be(2);
        result.ClinicRevenue[0].Arpu.Should().Be(100m);
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
