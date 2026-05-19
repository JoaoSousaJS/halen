using Halen.Infrastructure.Persistence;
using FluentAssertions;
using Halen.Application.Messaging.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class GetMyThreadsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetMyThreadsQueryHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _clinicId = TestTenantContext.DefaultClinicId;

        _patientUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();
        var otherDoctorUserId = Guid.NewGuid();

        var clinic = new Clinic { Id = _clinicId, Name = "Test Clinic", Slug = "test" };
        _db.Clinics.Add(clinic);

        _db.Users.AddRange(
            new User { Id = _patientUserId, ClinicId = _clinicId, FirstName = "Maya", LastName = "Chen", Role = UserRole.Patient, Email = "maya@test.com", UserName = "maya@test.com" },
            new User { Id = _doctorUserId, ClinicId = _clinicId, FirstName = "Amelia", LastName = "Chen", Role = UserRole.Doctor, Email = "dr.chen@test.com", UserName = "dr.chen@test.com" },
            new User { Id = otherDoctorUserId, ClinicId = _clinicId, FirstName = "Marcus", LastName = "Kim", Role = UserRole.Doctor, Email = "dr.kim@test.com", UserName = "dr.kim@test.com" });

        var doctorProfile = new DoctorProfile { Id = Guid.NewGuid(), UserId = _doctorUserId, ClinicId = _clinicId, Specialty = "Cardiology", LicenseNumber = "LIC-001", KycStatus = KycStatus.Approved };
        var otherDoctorProfile = new DoctorProfile { Id = Guid.NewGuid(), UserId = otherDoctorUserId, ClinicId = _clinicId, Specialty = "Dermatology", LicenseNumber = "LIC-002", KycStatus = KycStatus.Approved };
        _db.DoctorProfiles.AddRange(doctorProfile, otherDoctorProfile);

        var appt1 = new Appointment { Id = Guid.NewGuid(), ClinicId = _clinicId, PatientId = Guid.NewGuid(), DoctorId = doctorProfile.Id, ScheduledAt = DateTime.UtcNow, Reason = "Cardiology", Status = AppointmentStatus.Scheduled };
        var appt2 = new Appointment { Id = Guid.NewGuid(), ClinicId = _clinicId, PatientId = Guid.NewGuid(), DoctorId = otherDoctorProfile.Id, ScheduledAt = DateTime.UtcNow, Reason = "Dermatology", Status = AppointmentStatus.Completed };
        _db.Appointments.AddRange(appt1, appt2);

        _db.ConversationThreads.AddRange(
            new ConversationThread
            {
                ClinicId = _clinicId, AppointmentId = appt1.Id,
                PatientUserId = _patientUserId, DoctorUserId = _doctorUserId,
                Status = ThreadStatus.Active, Subject = "Cardiology consult",
                LastMessageAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                LastMessagePreview = "How are you?",
                PatientUnreadCount = 1
            },
            new ConversationThread
            {
                ClinicId = _clinicId, AppointmentId = appt2.Id,
                PatientUserId = _patientUserId, DoctorUserId = otherDoctorUserId,
                Status = ThreadStatus.Closed, Subject = "Dermatology follow-up",
                LastMessageAt = DateTimeOffset.UtcNow.AddDays(-1),
                LastMessagePreview = "Thread closed"
            });

        await _db.SaveChangesAsync();

        _handler = new GetMyThreadsQueryHandler(
            _db, Mock.Of<ILogger<GetMyThreadsQueryHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_PatientGetAllThreads_ReturnsBothThreads()
    {
        var query = new GetMyThreadsQuery(_patientUserId, UserRole.Patient, null, null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(2);
        result.Threads.Should().HaveCount(2);
        result.Threads[0].LastMessageAt.Should().BeAfter(result.Threads[1].LastMessageAt!.Value);
    }

    [TestMethod]
    public async Task Handle_FilterUnread_ReturnsOnlyUnreadThreads()
    {
        var query = new GetMyThreadsQuery(_patientUserId, UserRole.Patient, "unread", null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(1);
        result.Threads[0].Subject.Should().Contain("Cardiology");
    }

    [TestMethod]
    public async Task Handle_FilterClosed_ReturnsOnlyClosedThreads()
    {
        var query = new GetMyThreadsQuery(_patientUserId, UserRole.Patient, "closed", null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(1);
        result.Threads[0].Subject.Should().Contain("Dermatology");
    }

    [TestMethod]
    public async Task Handle_Search_MatchesSubject()
    {
        var query = new GetMyThreadsQuery(_patientUserId, UserRole.Patient, null, "Cardiology", 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Handle_DoctorGetsOnlyOwnThreads()
    {
        var query = new GetMyThreadsQuery(_doctorUserId, UserRole.Doctor, null, null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(1);
        result.Threads[0].Subject.Should().Contain("Cardiology");
    }

    [TestMethod]
    public async Task Handle_FilterNeedsReply_ReturnsActiveThreadsWithDoctorUnread()
    {
        var thread = _db.ConversationThreads.First(t => t.Subject.Contains("Cardiology"));
        thread.DoctorUnreadCount = 2;
        await _db.SaveChangesAsync();

        var query = new GetMyThreadsQuery(_doctorUserId, UserRole.Doctor, "needs_reply", null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(1);
        result.Threads[0].Subject.Should().Contain("Cardiology");
    }

    [TestMethod]
    public async Task Handle_FilterNeedsReply_ExcludesClosedThreads()
    {
        var thread = _db.ConversationThreads.First(t => t.Subject.Contains("Dermatology"));
        thread.DoctorUnreadCount = 1;
        await _db.SaveChangesAsync();

        var query = new GetMyThreadsQuery(_doctorUserId, UserRole.Doctor, "needs_reply", null, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Threads.Should().NotContain(t => t.Subject.Contains("Dermatology"));
    }

    [TestMethod]
    public async Task Handle_Pagination_RespectsPageSize()
    {
        var query = new GetMyThreadsQuery(_patientUserId, UserRole.Patient, null, null, 1, 1);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(2);
        result.Threads.Should().HaveCount(1);
    }
}
