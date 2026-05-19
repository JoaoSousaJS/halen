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
public class CloseThreadCommandHandlerTests
{
    private HalenDbContext _db = null!;
    private Mock<IEventBus> _eventBus = null!;
    private CloseThreadCommandHandler _handler = null!;
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

        _handler = new CloseThreadCommandHandler(
            _db, _eventBus.Object,
            Mock.Of<ILogger<CloseThreadCommandHandler>>());
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_DoctorCloses_SetsStatusAndCreatesSystemEvent()
    {
        var command = new CloseThreadCommand(_doctorUserId, _threadId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.Status.Should().Be(ThreadStatus.Closed);
        thread.ClosedAt.Should().NotBeNull();
        thread.ClosedByUserId.Should().Be(_doctorUserId);

        var systemMsg = await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.ThreadId == _threadId && m.MessageType == MessageType.SystemEvent);
        systemMsg.Should().NotBeNull();
        systemMsg!.Content.Should().Contain("closed");
    }

    [TestMethod]
    public async Task Handle_DoctorCloses_PublishesEvent()
    {
        var command = new CloseThreadCommand(_doctorUserId, _threadId);

        await _handler.Handle(command, CancellationToken.None);

        _eventBus.Verify(e => e.PublishAsync(
            Topics.ThreadClosed,
            It.Is<ThreadClosedEvent>(evt =>
                evt.ThreadId == _threadId &&
                evt.ClosedByUserId == _doctorUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_PatientTriesToClose_ReturnsForbidden()
    {
        var command = new CloseThreadCommand(_patientUserId, _threadId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task Handle_AlreadyClosed_ReturnsValidation()
    {
        var thread = await _db.ConversationThreads.FindAsync(_threadId);
        thread!.Status = ThreadStatus.Closed;
        await _db.SaveChangesAsync();

        var command = new CloseThreadCommand(_doctorUserId, _threadId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.Validation);
    }

    [TestMethod]
    public async Task Handle_ThreadNotFound_ReturnsNotFound()
    {
        var command = new CloseThreadCommand(_doctorUserId, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Kind.Should().Be(ErrorKind.NotFound);
    }
}
