using Halen.Infrastructure.Persistence;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Application.Messaging.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class MarkMessagesReadCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private MarkMessagesReadCommandHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _threadId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _eventBus = new Mock<IEventBus>();

        _patientUserId = Guid.NewGuid();
        _doctorUserId = Guid.NewGuid();

        var clinic = new Clinic { Id = TestTenantContext.DefaultClinicId, Name = "Test Clinic", Slug = "test" };
        _db.Clinics.Add(clinic);

        _db.Users.AddRange(
            new User { Id = _patientUserId, ClinicId = clinic.Id, FirstName = "Maya", LastName = "Chen", Role = UserRole.Patient, Email = "maya@test.com", UserName = "maya@test.com" },
            new User { Id = _doctorUserId, ClinicId = clinic.Id, FirstName = "Amelia", LastName = "Chen", Role = UserRole.Doctor, Email = "dr.chen@test.com", UserName = "dr.chen@test.com" });

        var thread = new ConversationThread
        {
            Id = Guid.NewGuid(),
            ClinicId = clinic.Id,
            AppointmentId = Guid.NewGuid(),
            PatientUserId = _patientUserId,
            DoctorUserId = _doctorUserId,
            Status = ThreadStatus.Active,
            Subject = "Cardiology consult",
            PatientUnreadCount = 2
        };
        _threadId = thread.Id;
        _db.ConversationThreads.Add(thread);

        _db.ChatMessages.AddRange(
            new ChatMessage { ClinicId = clinic.Id, ThreadId = _threadId, SenderUserId = _doctorUserId, Content = "Hello", MessageType = MessageType.Text, IsRead = false },
            new ChatMessage { ClinicId = clinic.Id, ThreadId = _threadId, SenderUserId = _doctorUserId, Content = "How are you?", MessageType = MessageType.Text, IsRead = false },
            new ChatMessage { ClinicId = clinic.Id, ThreadId = _threadId, SenderUserId = _patientUserId, Content = "My own message", MessageType = MessageType.Text, IsRead = false });

        await _db.SaveChangesAsync();

        _handler = new MarkMessagesReadCommandHandler(
            _db, _eventBus.Object,
            Mock.Of<ILogger<MarkMessagesReadCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_PatientMarksRead_OnlyMarksDoctorMessages()
    {
        var command = new MarkMessagesReadCommand(_patientUserId, _threadId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var messages = await _db.ChatMessages.Where(m => m.ThreadId == _threadId).ToListAsync();
        var doctorMessages = messages.Where(m => m.SenderUserId == _doctorUserId).ToList();
        var patientMessages = messages.Where(m => m.SenderUserId == _patientUserId).ToList();

        doctorMessages.Should().AllSatisfy(m =>
        {
            m.IsRead.Should().BeTrue();
            m.ReadAt.Should().NotBeNull();
        });

        patientMessages.Should().AllSatisfy(m => m.IsRead.Should().BeFalse());
    }

    [TestMethod]
    public async Task Handle_PatientMarksRead_ResetsUnreadCount()
    {
        var command = new MarkMessagesReadCommand(_patientUserId, _threadId);

        await _handler.Handle(command, CancellationToken.None);

        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.PatientUnreadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_MarksRead_PublishesEvent()
    {
        var command = new MarkMessagesReadCommand(_patientUserId, _threadId);

        await _handler.Handle(command, CancellationToken.None);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.MessagesRead,
            It.Is<MessagesReadEvent>(evt =>
                evt.ThreadId == _threadId &&
                evt.ReadByUserId == _patientUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_NonParticipant_ReturnsForbidden()
    {
        var outsiderId = Guid.NewGuid();
        _db.Users.Add(new User { Id = outsiderId, ClinicId = TestTenantContext.DefaultClinicId, FirstName = "Outsider", LastName = "User", Role = UserRole.Patient, Email = "outsider@test.com", UserName = "outsider@test.com" });
        await _db.SaveChangesAsync();

        var command = new MarkMessagesReadCommand(outsiderId, _threadId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }
}
