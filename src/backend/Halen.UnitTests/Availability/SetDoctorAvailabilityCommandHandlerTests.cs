using FluentAssertions;
using Halen.Application.Availability.Commands;
using Halen.Application.Common;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Halen.UnitTests.Availability;

[TestClass]
public class SetDoctorAvailabilityCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private TestTenantContext _tenantContext = null!;
    private SetDoctorAvailabilityCommandHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _doctorProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _tenantContext = new TestTenantContext();

        _doctorUserId = Guid.NewGuid();
        var doctorUser = new User
        {
            Id = _doctorUserId,
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
            UserId = _doctorUserId,
            Specialty = "Diagnostics",
            LicenseNumber = "LIC-001",
            ConsultationFee = 150,
            YearsOfExperience = 10,
            KycStatus = KycStatus.Approved,
        };
        _db.DoctorProfiles.Add(doctorProfile);
        _doctorProfileId = doctorProfile.Id;

        await _db.SaveChangesAsync();

        _handler = new SetDoctorAvailabilityCommandHandler(_db, _tenantContext);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidSlots_ReplacesExistingSchedule()
    {
        // Arrange: seed an existing availability window
        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = _tenantContext.ClinicId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var newSlots = new List<AvailabilitySlotDto>
        {
            new(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            new(DayOfWeek.Wednesday, new TimeOnly(14, 0), new TimeOnly(17, 0)),
        };

        var command = new SetDoctorAvailabilityCommand(_doctorUserId, newSlots);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();

        var remaining = await _db.DoctorAvailabilities
            .Where(a => a.DoctorProfileId == _doctorProfileId)
            .ToListAsync();

        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(a => a.DayOfWeek == DayOfWeek.Monday);
        remaining.Should().Contain(a => a.DayOfWeek == DayOfWeek.Tuesday);
        remaining.Should().Contain(a => a.DayOfWeek == DayOfWeek.Wednesday);
    }

    [TestMethod]
    public async Task Handle_DoctorNotFound_ReturnsNotFoundError()
    {
        var command = new SetDoctorAvailabilityCommand(
            Guid.NewGuid(),
            new List<AvailabilitySlotDto>
            {
                new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Doctor profile not found.");
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_OverlappingWindowsOnSameDay_ReturnsValidationError()
    {
        var slots = new List<AvailabilitySlotDto>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0)),
            new(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(12, 0)),
        };

        var command = new SetDoctorAvailabilityCommand(_doctorUserId, slots);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Overlapping slots detected on Monday");
        result.Kind.Should().Be(ErrorKind.Validation);
    }

    [TestMethod]
    public async Task Handle_EmptySlotsList_ClearsAllExistingAvailability()
    {
        // Arrange: seed existing availability
        _db.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfileId = _doctorProfileId,
            ClinicId = _tenantContext.ClinicId,
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 20,
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var command = new SetDoctorAvailabilityCommand(_doctorUserId, new List<AvailabilitySlotDto>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var remaining = await _db.DoctorAvailabilities
            .Where(a => a.DoctorProfileId == _doctorProfileId)
            .ToListAsync();

        remaining.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_MultipleDays_HandlesSlotsAcrossDifferentDays()
    {
        var slots = new List<AvailabilitySlotDto>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            new(DayOfWeek.Wednesday, new TimeOnly(14, 0), new TimeOnly(17, 0)),
            new(DayOfWeek.Friday, new TimeOnly(8, 0), new TimeOnly(11, 0)),
        };

        var command = new SetDoctorAvailabilityCommand(_doctorUserId, slots);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var saved = await _db.DoctorAvailabilities
            .Where(a => a.DoctorProfileId == _doctorProfileId)
            .ToListAsync();

        saved.Should().HaveCount(3);
        saved.Select(a => a.DayOfWeek).Should().BeEquivalentTo(
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });

        // Verify all entries got the correct ClinicId and default slot duration
        saved.Should().AllSatisfy(a =>
        {
            a.ClinicId.Should().Be(_tenantContext.ClinicId);
            a.SlotDurationMinutes.Should().Be(20);
            a.IsActive.Should().BeTrue();
        });
    }
}
