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
public class SendAttachmentCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IFileStorage> _fileStorage = null!;
    private Mock<IEventBus> _eventBus = null!;
    private SendAttachmentCommandHandler _handler = null!;
    private Guid _patientUserId;
    private Guid _doctorUserId;
    private Guid _threadId;

    [TestInitialize]
    public async Task Initialize()
    {
        _db = TestDbFactory.Create();
        _fileStorage = new Mock<IFileStorage>();
        _eventBus = new Mock<IEventBus>();

        _fileStorage.Setup(f => f.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("uploads/test-path.png");

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

        _handler = new SendAttachmentCommandHandler(
            _db, _fileStorage.Object, _eventBus.Object,
            Mock.Of<ILogger<SendAttachmentCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    private static Stream CreatePngStream()
    {
        var ms = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        ms.Position = 0;
        return ms;
    }

    private static Stream CreateJpegStream()
    {
        var ms = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 });
        ms.Position = 0;
        return ms;
    }

    [TestMethod]
    public async Task Handle_ValidPngAttachment_CreatesMessageAndAttachment()
    {
        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "xray.png", "image/png", 1024, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNull();

        var msg = await _db.ChatMessages.FindAsync(result.MessageId);
        msg.Should().NotBeNull();
        msg!.MessageType.Should().Be(MessageType.Attachment);
        msg.Content.Should().Contain("xray.png");

        var attachments = _db.MessageAttachments.Where(a => a.MessageId == msg.Id).ToList();
        attachments.Should().HaveCount(1);
        attachments[0].ContentType.Should().Be("image/png");
        attachments[0].AttachmentType.Should().Be(MessageAttachmentType.Image);
        attachments[0].StoragePath.Should().Be("uploads/test-path.png");
    }

    [TestMethod]
    public async Task Handle_ValidAttachment_IncrementsRecipientUnreadCount()
    {
        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "photo.png", "image/png", 512, stream);

        await _handler.Handle(command, CancellationToken.None);

        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.DoctorUnreadCount.Should().Be(1);
        thread.PatientUnreadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_DoctorSendsAttachment_IncrementsPatientUnreadCount()
    {
        using var stream = CreateJpegStream();
        var command = new SendAttachmentCommand(
            _doctorUserId, _threadId, "results.jpg", "image/jpeg", 2048, stream);

        await _handler.Handle(command, CancellationToken.None);

        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.PatientUnreadCount.Should().Be(1);
        thread.DoctorUnreadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_ValidAttachment_PublishesEvent()
    {
        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "scan.png", "image/png", 1024, stream);

        await _handler.Handle(command, CancellationToken.None);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.MessageSent,
            It.Is<MessageSentEvent>(evt =>
                evt.ThreadId == _threadId &&
                evt.SenderUserId == _patientUserId &&
                evt.RecipientUserId == _doctorUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_NonParticipant_ReturnsForbidden()
    {
        var outsiderId = Guid.NewGuid();
        _db.Users.Add(new User { Id = outsiderId, ClinicId = TestTenantContext.DefaultClinicId, FirstName = "Outsider", LastName = "User", Role = UserRole.Patient, Email = "outsider@test.com", UserName = "outsider@test.com" });
        await _db.SaveChangesAsync();

        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            outsiderId, _threadId, "sneaky.png", "image/png", 1024, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_ClosedThread_ReturnsValidationError()
    {
        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.Status = ThreadStatus.Closed;
        await _db.SaveChangesAsync();

        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "late.png", "image/png", 1024, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }

    [TestMethod]
    public async Task Handle_ThreadNotFound_ReturnsNotFound()
    {
        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            _patientUserId, Guid.NewGuid(), "photo.png", "image/png", 1024, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }

    [TestMethod]
    public async Task Handle_PdfAttachment_ClassifiedAsDocument()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E };
        using var stream = new MemoryStream(pdfBytes);
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "report.pdf", "application/pdf", 5000, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var attachment = _db.MessageAttachments.First(a => a.MessageId == result.MessageId);
        attachment.AttachmentType.Should().Be(MessageAttachmentType.Document);
    }

    [TestMethod]
    public async Task Handle_WebmAttachment_ClassifiedAsVoiceMemo()
    {
        using var stream = new MemoryStream(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x00 });
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "recording.webm", "audio/webm", 3000, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var attachment = _db.MessageAttachments.First(a => a.MessageId == result.MessageId);
        attachment.AttachmentType.Should().Be(MessageAttachmentType.VoiceMemo);
    }

    [TestMethod]
    public async Task Handle_MagicBytesMismatch_ReturnsValidationError()
    {
        var htmlBytes = System.Text.Encoding.UTF8.GetBytes("<html><body>evil</body></html>");
        using var stream = new MemoryStream(htmlBytes);
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "evil.png", "image/png", htmlBytes.Length, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("content does not match");
    }

    [TestMethod]
    public async Task Handle_SanitizesFileName()
    {
        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "../../../etc/passwd.png", "image/png", 1024, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var attachment = _db.MessageAttachments.First(a => a.MessageId == result.MessageId);
        attachment.FileName.Should().NotContain("..");
    }

    [TestMethod]
    public async Task Handle_RateLimitExceeded_ReturnsValidation()
    {
        for (int i = 0; i < 5; i++)
        {
            _db.ChatMessages.Add(new ChatMessage
            {
                ClinicId = TestTenantContext.DefaultClinicId,
                ThreadId = _threadId,
                SenderUserId = _patientUserId,
                Content = $"Attachment {i}",
                MessageType = MessageType.Attachment,
            });
        }
        await _db.SaveChangesAsync();

        using var stream = CreatePngStream();
        var command = new SendAttachmentCommand(
            _patientUserId, _threadId, "onemore.png", "image/png", 1024, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Should().Contain("rate limit");
    }
}
