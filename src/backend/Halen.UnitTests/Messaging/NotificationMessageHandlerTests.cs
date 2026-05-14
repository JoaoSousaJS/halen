using System.Text.Json;
using FluentAssertions;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Halen.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;

namespace Halen.UnitTests.Messaging;

[TestClass]
public class NotificationMessageHandlerTests
{
    private Mock<INotificationSender> _sender = null!;
    private NotificationMessageHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _sender = new Mock<INotificationSender>();
        _handler = new NotificationMessageHandler(
            _sender.Object,
            Mock.Of<ILogger<NotificationMessageHandler>>());
    }

    [TestMethod]
    public async Task HandleAsync_AppointmentBooked_NotifiesDoctor()
    {
        var evt = new AppointmentBookedEvent(
            Guid.NewGuid(),
            PatientUserId: Guid.NewGuid(),
            DoctorUserId: Guid.Parse("aaaa0000-0000-0000-0000-000000000001"),
            DateTime.UtcNow.AddDays(1),
            "John Doe",
            "Dr. House");

        await _handler.HandleAsync("appointment.booked", JsonSerializer.Serialize(evt), CancellationToken.None);

        _sender.Verify(s => s.SendToUserAsync(
            evt.DoctorUserId.ToString(),
            It.Is<NotificationDto>(n =>
                n.Type == "appointment.booked" &&
                n.Message.Contains("John Doe")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_AppointmentCancelledByPatient_NotifiesDoctor()
    {
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var evt = new AppointmentCancelledEvent(
            Guid.NewGuid(),
            CancelledByUserId: patientId,
            PatientUserId: patientId,
            DoctorUserId: doctorId,
            "John Doe",
            "Patient");

        await _handler.HandleAsync("appointment.cancelled", JsonSerializer.Serialize(evt), CancellationToken.None);

        _sender.Verify(s => s.SendToUserAsync(
            doctorId.ToString(),
            It.Is<NotificationDto>(n => n.Type == "appointment.cancelled"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_AppointmentCancelledByDoctor_NotifiesPatient()
    {
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var evt = new AppointmentCancelledEvent(
            Guid.NewGuid(),
            CancelledByUserId: doctorId,
            PatientUserId: patientId,
            DoctorUserId: doctorId,
            "Dr. House",
            "Doctor");

        await _handler.HandleAsync("appointment.cancelled", JsonSerializer.Serialize(evt), CancellationToken.None);

        _sender.Verify(s => s.SendToUserAsync(
            patientId.ToString(),
            It.Is<NotificationDto>(n => n.Type == "appointment.cancelled"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_AppointmentCompleted_NotifiesPatient()
    {
        var patientId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

        var evt = new AppointmentCompletedEvent(
            Guid.NewGuid(),
            DoctorUserId: Guid.NewGuid(),
            PatientUserId: patientId,
            "Dr. House");

        await _handler.HandleAsync("appointment.completed", JsonSerializer.Serialize(evt), CancellationToken.None);

        _sender.Verify(s => s.SendToUserAsync(
            patientId.ToString(),
            It.Is<NotificationDto>(n =>
                n.Type == "appointment.completed" &&
                n.Message.Contains("Dr. House")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_UnknownTopic_DoesNotSendNotification()
    {
        await _handler.HandleAsync("unknown.topic", "{}", CancellationToken.None);

        _sender.Verify(s => s.SendToUserAsync(
            It.IsAny<string>(),
            It.IsAny<NotificationDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
