using Halen.Infrastructure.Persistence;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Messaging.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class GetThreadMessagesQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private GetThreadMessagesQueryHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _threadId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        var clinicId = TestTenantContext.DefaultClinicId;

        _patientUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();

        var clinic = new Clinic { Id = clinicId, Name = "Test Clinic", Slug = "test" };
        _db.Clinics.Add(clinic);

        _db.Users.AddRange(
            new User { Id = _patientUserId, ClinicId = clinicId, FirstName = "Maya", LastName = "Chen", Role = UserRole.Patient, Email = "maya@test.com", UserName = "maya@test.com" },
            new User { Id = _doctorUserId, ClinicId = clinicId, FirstName = "Amelia", LastName = "Chen", Role = UserRole.Doctor, Email = "dr.chen@test.com", UserName = "dr.chen@test.com" });

        var thread = new ConversationThread
        {
            Id = Guid.NewGuid(), ClinicId = clinicId, AppointmentId = Guid.NewGuid(),
            PatientUserId = _patientUserId, DoctorUserId = _doctorUserId,
            Status = ThreadStatus.Active, Subject = "Test thread"
        };
        _threadId = thread.Id;
        _db.ConversationThreads.Add(thread);

        _db.ChatMessages.AddRange(
            new ChatMessage { ClinicId = clinicId, ThreadId = _threadId, SenderUserId = _doctorUserId, Content = "Hello", MessageType = MessageType.Text },
            new ChatMessage { ClinicId = clinicId, ThreadId = _threadId, SenderUserId = _patientUserId, Content = "Hi doctor", MessageType = MessageType.Text },
            new ChatMessage { ClinicId = clinicId, ThreadId = _threadId, SenderUserId = _doctorUserId, Content = "How are you?", MessageType = MessageType.Text });

        await _db.SaveChangesAsync();

        _handler = new GetThreadMessagesQueryHandler(
            _db, Mock.Of<ILogger<GetThreadMessagesQueryHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidParticipant_ReturnsMessages()
    {
        var query = new GetThreadMessagesQuery(_patientUserId, _threadId, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(3);
        result.Messages.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task Handle_MessagesIncludeSenderInfo()
    {
        var query = new GetThreadMessagesQuery(_patientUserId, _threadId, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        var doctorMsg = result.Messages.First(m => m.SenderUserId == _doctorUserId);
        doctorMsg.SenderName.Should().Contain("Amelia");
        doctorMsg.SenderRole.Should().Be(UserRole.Doctor);
    }

    [TestMethod]
    public async Task Handle_NonParticipant_ReturnsForbidden()
    {
        var outsiderId = Guid.NewGuid();
        _db.Users.Add(new User { Id = outsiderId, ClinicId = TestTenantContext.DefaultClinicId, FirstName = "Outsider", LastName = "User", Role = UserRole.Patient, Email = "outsider@test.com", UserName = "outsider@test.com" });
        await _db.SaveChangesAsync();

        var query = new GetThreadMessagesQuery(outsiderId, _threadId, 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_Pagination_RespectsPageSize()
    {
        var query = new GetThreadMessagesQuery(_patientUserId, _threadId, 1, 2);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(3);
        result.Messages.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Handle_ThreadNotFound_ReturnsNotFound()
    {
        var query = new GetThreadMessagesQuery(_patientUserId, Guid.NewGuid(), 1, 50);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}
