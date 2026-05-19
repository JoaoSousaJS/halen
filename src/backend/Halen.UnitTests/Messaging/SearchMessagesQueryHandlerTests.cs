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
public class SearchMessagesQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private SearchMessagesQueryHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _threadId;
    private Guid _clinicId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _clinicId = TestTenantContext.DefaultClinicId;

        _patientUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();

        var clinic = new Clinic { Id = _clinicId, Name = "Test Clinic", Slug = "test" };
        _db.Clinics.Add(clinic);

        _db.Users.AddRange(
            new User { Id = _patientUserId, ClinicId = _clinicId, FirstName = "Maya", LastName = "Chen", Role = UserRole.Patient, Email = "maya@test.com", UserName = "maya@test.com" },
            new User { Id = _doctorUserId, ClinicId = _clinicId, FirstName = "Amelia", LastName = "Chen", Role = UserRole.Doctor, Email = "dr.chen@test.com", UserName = "dr.chen@test.com" });

        var doctorProfile = new DoctorProfile { Id = Guid.NewGuid(), UserId = _doctorUserId, ClinicId = _clinicId, Specialty = "Cardiology", LicenseNumber = "LIC-001", KycStatus = KycStatus.Approved };
        _db.DoctorProfiles.Add(doctorProfile);

        var appt = new Appointment { Id = Guid.NewGuid(), ClinicId = _clinicId, PatientId = Guid.NewGuid(), DoctorId = doctorProfile.Id, ScheduledAt = DateTime.UtcNow, Reason = "Test", Status = AppointmentStatus.Scheduled };
        _db.Appointments.Add(appt);

        var thread = new ConversationThread
        {
            Id = Guid.NewGuid(),
            ClinicId = _clinicId,
            AppointmentId = appt.Id,
            PatientUserId = _patientUserId,
            DoctorUserId = _doctorUserId,
            Status = ThreadStatus.Active,
            Subject = "Test thread"
        };
        _threadId = thread.Id;
        _db.ConversationThreads.Add(thread);

        _db.ChatMessages.AddRange(
            new ChatMessage { ClinicId = _clinicId, ThreadId = _threadId, SenderUserId = _doctorUserId, Content = "How is your chest pain?", MessageType = MessageType.Text },
            new ChatMessage { ClinicId = _clinicId, ThreadId = _threadId, SenderUserId = _patientUserId, Content = "Much better after the medication", MessageType = MessageType.Text },
            new ChatMessage { ClinicId = _clinicId, ThreadId = _threadId, SenderUserId = _doctorUserId, Content = "Thread opened", MessageType = MessageType.SystemEvent });

        await _db.SaveChangesAsync();

        _handler = new SearchMessagesQueryHandler(
            _db, Mock.Of<ILogger<SearchMessagesQueryHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_MatchingQuery_ReturnsHits()
    {
        var query = new SearchMessagesQuery(_patientUserId, "chest pain", 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(1);
        result.Hits.Should().HaveCount(1);
        result.Hits[0].Content.Should().Contain("chest pain");
    }

    [TestMethod]
    public async Task Handle_NoMatch_ReturnsEmpty()
    {
        var query = new SearchMessagesQuery(_patientUserId, "headache", 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(0);
        result.Hits.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_ExcludesSystemEvents()
    {
        var query = new SearchMessagesQuery(_patientUserId, "Thread opened", 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_NonParticipant_ReturnsEmpty()
    {
        var outsiderId = Guid.NewGuid();
        _db.Users.Add(new User { Id = outsiderId, ClinicId = _clinicId, FirstName = "Outsider", LastName = "User", Role = UserRole.Patient, Email = "outsider@test.com", UserName = "outsider@test.com" });
        await _db.SaveChangesAsync();

        var query = new SearchMessagesQuery(outsiderId, "chest", 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_Pagination_RespectsPageSize()
    {
        var query = new SearchMessagesQuery(_patientUserId, "the", 1, 1);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Hits.Should().HaveCount(1);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
