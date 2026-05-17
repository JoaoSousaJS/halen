using Halen.UnitTests.Helpers;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Consultations.Queries;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Consultations;

[TestClass]
public class GetConsultationRoomQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetConsultationRoomQueryHandler _handler = null!;
    private Guid _doctorUserId;
    private Guid _patientUserId;
    private Guid _appointmentId;
    private Guid _patientProfileId;
    private Guid _doctorProfileId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();

        _doctorUserId = Guid.NewGuid();
        _patientUserId = Guid.NewGuid();
        _patientProfileId = Guid.NewGuid();
        _doctorProfileId = Guid.NewGuid();
        _appointmentId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = _patientUserId, FirstName = "Pat", LastName = "Ient", Email = "pat@test.com", UserName = "pat@test.com", Role = UserRole.Patient },
            new User { Id = _doctorUserId, FirstName = "Dr", LastName = "House", Email = "doc@test.com", UserName = "doc@test.com", Role = UserRole.Doctor }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = _patientProfileId, UserId = _patientUserId });
        _db.DoctorProfiles.Add(new DoctorProfile { Id = _doctorProfileId, UserId = _doctorUserId, Specialty = "Diagnostics", LicenseNumber = "LIC-001", ConsultationFee = 100, YearsOfExperience = 5 });

        _db.Appointments.Add(new Appointment
        {
            Id = _appointmentId,
            PatientId = _patientProfileId,
            DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddMinutes(5),
            DurationMinutes = 30,
            Reason = "Checkup",
            Status = AppointmentStatus.Scheduled,
        });

        _db.ConsultationRooms.Add(new ConsultationRoom
        {
            AppointmentId = _appointmentId,
            ClinicId = TestTenantContext.DefaultClinicId,
            RoomCode = "VC-ABCD",
            Status = ConsultationRoomStatus.Waiting,
            Notes = "Initial notes",
        });

        await _db.SaveChangesAsync();

        _handler = new GetConsultationRoomQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ReturnsRoomDto_WithCorrectFields()
    {
        var query = new GetConsultationRoomQuery(_doctorUserId, _appointmentId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Room.Should().NotBeNull();
        result.Room!.AppointmentId.Should().Be(_appointmentId);
        result.Room.RoomCode.Should().Be("VC-ABCD");
        result.Room.Status.Should().Be(ConsultationRoomStatus.Waiting.ToString());
        result.Room.DoctorName.Should().Be("Dr House");
        result.Room.PatientName.Should().Be("Pat Ient");
        result.Room.Reason.Should().Be("Checkup");
        result.Room.DurationMinutes.Should().Be(30);
        result.Room.Notes.Should().Be("Initial notes");
    }

    [TestMethod]
    public async Task Handle_NoRoomExists_ReturnsNotFound()
    {
        var otherAppointmentId = Guid.NewGuid();
        _db.Appointments.Add(new Appointment
        {
            Id = otherAppointmentId,
            PatientId = _patientProfileId,
            DoctorId = _doctorProfileId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Reason = "Follow-up",
            Status = AppointmentStatus.Scheduled,
        });
        await _db.SaveChangesAsync();

        var query = new GetConsultationRoomQuery(_doctorUserId, otherAppointmentId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_CallerIsNotParticipant_ReturnsForbidden()
    {
        var unrelatedUserId = Guid.NewGuid();
        _db.Users.Add(new User { Id = unrelatedUserId, FirstName = "Random", LastName = "User", Email = "rando@test.com", UserName = "rando@test.com", Role = UserRole.Patient });
        _db.PatientProfiles.Add(new PatientProfile { Id = Guid.NewGuid(), UserId = unrelatedUserId });
        await _db.SaveChangesAsync();

        var query = new GetConsultationRoomQuery(unrelatedUserId, _appointmentId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_PatientCanQueryRoom_Succeeds()
    {
        var query = new GetConsultationRoomQuery(_patientUserId, _appointmentId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Room.Should().NotBeNull();
    }
}
