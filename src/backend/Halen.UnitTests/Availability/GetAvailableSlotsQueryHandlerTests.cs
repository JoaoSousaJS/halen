using FluentAssertions;
using Halen.Application.Availability.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Availability;

[TestClass]
public class GetAvailableSlotsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetAvailableSlotsQueryHandler _handler = null!;
    private Guid _doctorProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        var doctorUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Dr",
            LastName = "House",
            Email = "house@test.com",
            UserName = "house@test.com",
            Role = UserRole.Doctor,
        };
        _db.Users.Add(doctorUser);

        var doctorProfile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = doctorUser.Id,
            Specialty = "Diagnostics",
            LicenseNumber = "LIC-001",
            ConsultationFee = 150,
            YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(doctorProfile);
        _doctorProfileId = doctorProfile.Id;

        await _db.SaveChangesAsync();

        _handler = new GetAvailableSlotsQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_AvailabilityWindow_GeneratesCorrectSlots()
    {
        // 09:00 - 10:00 with 20-min slots = 3 slots (09:00, 09:20, 09:40)
        // A future date that falls on the target day of week
        var targetDate = GetNextDateForDayOfWeek(DayOfWeek.Monday);

        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = targetDate.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var query = new GetAvailableSlotsQuery(_doctorProfileId, targetDate);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Slots.Should().HaveCount(3);
        result.Slots[0].StartLocal.Should().Be("09:00");
        result.Slots[1].StartLocal.Should().Be("09:20");
        result.Slots[2].StartLocal.Should().Be("09:40");
        result.Slots.Should().AllSatisfy(s => s.IsAvailable.Should().BeTrue());
    }

    [TestMethod]
    public async Task Handle_SlotsOverlappingWithBookedAppointments_FilteredAsUnavailable()
    {
        var targetDate = GetNextDateForDayOfWeek(DayOfWeek.Tuesday);

        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = targetDate.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });

        // Book the 09:00 slot
        var patientUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            UserName = "john@test.com",
            Role = UserRole.Patient,
        };
        _db.Users.Add(patientUser);
        var patientProfile = new PatientProfile { Id = Guid.NewGuid(), UserId = patientUser.Id };
        _db.PatientProfiles.Add(patientProfile);

        _db.Appointments.Add(new Appointment
        {
            PatientId = patientProfile.Id,
            DoctorId = _doctorProfileId,
            ScheduledAt = targetDate.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
            DurationMinutes = 20,
            Reason = "Check-up",
            Status = AppointmentStatus.Scheduled,
        });
        await _db.SaveChangesAsync();

        var query = new GetAvailableSlotsQuery(_doctorProfileId, targetDate);

        var result = await _handler.Handle(query, CancellationToken.None);

        // 3 total slots, but the 09:00 one should be marked as not available
        result.Slots.Should().HaveCount(3);
        result.Slots.First(s => s.StartLocal == "09:00").IsAvailable.Should().BeFalse();
        result.Slots.First(s => s.StartLocal == "09:20").IsAvailable.Should().BeTrue();
        result.Slots.First(s => s.StartLocal == "09:40").IsAvailable.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_DateIsToday_FiltersPastSlots()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Place the window 2 hours before now so most/all slots are past.
        // For the midnight hour, fall back to 00:00 — the assertion below
        // handles both cases by checking no past slot leaks through.
        var safeHour = Math.Max(DateTime.UtcNow.Hour - 2, 0);

        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = today.DayOfWeek,
            StartTime = new TimeOnly(safeHour, 0),
            EndTime = new TimeOnly(safeHour, 40),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var query = new GetAvailableSlotsQuery(_doctorProfileId, today);

        var result = await _handler.Handle(query, CancellationToken.None);

        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        result.Slots.Should().NotContain(
            s => TimeOnly.Parse(s.StartLocal) < now,
            "past slots should be excluded from today's availability");
    }

    [TestMethod]
    public async Task Handle_NoAvailabilityForDay_ReturnsEmptyList()
    {
        // Availability is on Monday, query for Tuesday
        var monday = GetNextDateForDayOfWeek(DayOfWeek.Monday);

        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var tuesday = GetNextDateForDayOfWeek(DayOfWeek.Tuesday);
        var query = new GetAvailableSlotsQuery(_doctorProfileId, tuesday);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Slots.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_AllSlotsBooked_ReturnsAllUnavailable()
    {
        var targetDate = GetNextDateForDayOfWeek(DayOfWeek.Wednesday);

        // 09:00 - 09:40 = 2 slots (09:00, 09:20)
        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = TestTenantContext.DefaultClinicId,
            DayOfWeek = targetDate.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 40),
            SlotDurationMinutes = 20,
            IsActive = true,
        });

        var patientUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            UserName = "jane@test.com",
            Role = UserRole.Patient,
        };
        _db.Users.Add(patientUser);
        var patientProfile = new PatientProfile { Id = Guid.NewGuid(), UserId = patientUser.Id };
        _db.PatientProfiles.Add(patientProfile);

        // Book both slots
        _db.Appointments.Add(new Appointment
        {
            PatientId = patientProfile.Id,
            DoctorId = _doctorProfileId,
            ScheduledAt = targetDate.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
            DurationMinutes = 20,
            Reason = "Visit 1",
            Status = AppointmentStatus.Scheduled,
        });
        _db.Appointments.Add(new Appointment
        {
            PatientId = patientProfile.Id,
            DoctorId = _doctorProfileId,
            ScheduledAt = targetDate.ToDateTime(new TimeOnly(9, 20), DateTimeKind.Utc),
            DurationMinutes = 20,
            Reason = "Visit 2",
            Status = AppointmentStatus.Scheduled,
        });
        await _db.SaveChangesAsync();

        var query = new GetAvailableSlotsQuery(_doctorProfileId, targetDate);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Slots.Should().HaveCount(2);
        result.Slots.Should().AllSatisfy(s => s.IsAvailable.Should().BeFalse());
    }

    /// <summary>
    /// Returns the next occurrence of the given day of week that is at least 7 days in the future,
    /// guaranteeing it's never "today" (which would trigger past-slot filtering).
    /// </summary>
    private static DateOnly GetNextDateForDayOfWeek(DayOfWeek target)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Start from 7 days out to ensure the date is safely in the future
        var candidate = today.AddDays(7);
        while (candidate.DayOfWeek != target)
            candidate = candidate.AddDays(1);
        return candidate;
    }
}
