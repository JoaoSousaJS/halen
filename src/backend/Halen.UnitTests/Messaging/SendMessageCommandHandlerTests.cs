using Halen.Infrastructure.Persistence;
using FluentAssertions;
using Halen.Application.Common;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Application.Messaging.Commands;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class SendMessageCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private SendMessageCommandHandler _handler = null!;
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
            Subject = "Cardiology consult"
        };
        _threadId = thread.Id;
        _db.ConversationThreads.Add(thread);
        await _db.SaveChangesAsync();

        _handler = new SendMessageCommandHandler(
            _db, _eventBus.Object,
            Mock.Of<ILogger<SendMessageCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ValidMessage_CreatesMessageAndUpdatesThread()
    {
        var command = new SendMessageCommand(_patientUserId, _threadId, "Hello doctor!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNull();

        var msg = await _db.ChatMessages.FindAsync(result.MessageId);
        msg.Should().NotBeNull();
        msg!.Content.Should().Be("Hello doctor!");
        msg.MessageType.Should().Be(MessageType.Text);
        msg.SenderUserId.Should().Be(_patientUserId);
        msg.ThreadId.Should().Be(_threadId);
        msg.IsRead.Should().BeFalse();

        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.LastMessagePreview.Should().Be("Hello doctor!");
        thread.LastMessageAt.Should().NotBeNull();
        thread.DoctorUnreadCount.Should().Be(1);
        thread.PatientUnreadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_DoctorSendsMessage_IncrementsPatientUnreadCount()
    {
        var command = new SendMessageCommand(_doctorUserId, _threadId, "How are you?");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.PatientUnreadCount.Should().Be(1);
        thread.DoctorUnreadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_ValidMessage_PublishesEvent()
    {
        var command = new SendMessageCommand(_patientUserId, _threadId, "Test message");

        await _handler.Handle(command, CancellationToken.None);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.MessageSent,
            It.Is<MessageSentEvent>(evt =>
                evt.ThreadId == _threadId &&
                evt.SenderUserId == _patientUserId &&
                evt.RecipientUserId == _doctorUserId &&
                evt.Preview == "Test message"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_NonParticipant_ReturnsForbidden()
    {
        var outsiderId = Guid.NewGuid();
        _db.Users.Add(new User { Id = outsiderId, ClinicId = TestTenantContext.DefaultClinicId, FirstName = "Outsider", LastName = "User", Role = UserRole.Patient, Email = "outsider@test.com", UserName = "outsider@test.com" });
        await _db.SaveChangesAsync();

        var command = new SendMessageCommand(outsiderId, _threadId, "Sneaky message");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);

        _eventBus.Verify(e => e.PublishAsync(
            It.IsAny<string>(), It.IsAny<MessageSentEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ClosedThread_ReturnsValidationError()
    {
        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.Status = ThreadStatus.Closed;
        await _db.SaveChangesAsync();

        var command = new SendMessageCommand(_patientUserId, _threadId, "Late message");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }

    [TestMethod]
    public async Task Handle_ThreadNotFound_ReturnsNotFound()
    {
        var command = new SendMessageCommand(_patientUserId, Guid.NewGuid(), "Hello");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_ContentTrimmed_StoresCleanContent()
    {
        var command = new SendMessageCommand(_patientUserId, _threadId, "  Hello doctor!  ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var msg = await _db.ChatMessages.FindAsync(result.MessageId);
        msg!.Content.Should().Be("Hello doctor!");
    }

    [TestMethod]
    public async Task Handle_LongPreview_TruncatesTo200Chars()
    {
        var longMessage = new string('A', 500);
        var command = new SendMessageCommand(_patientUserId, _threadId, longMessage);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.LastMessagePreview!.Length.Should().BeLessThanOrEqualTo(200);
    }

    [TestMethod]
    public async Task Handle_RateLimitExceeded_ReturnsValidation()
    {
        for (int i = 0; i < 10; i++)
        {
            _db.ChatMessages.Add(new ChatMessage
            {
                ClinicId = TestTenantContext.DefaultClinicId,
                ThreadId = _threadId,
                SenderUserId = _patientUserId,
                Content = $"Message {i}",
                MessageType = MessageType.Text,
            });
        }
        await _db.SaveChangesAsync();

        var command = new SendMessageCommand(_patientUserId, _threadId, "One more");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }
}
