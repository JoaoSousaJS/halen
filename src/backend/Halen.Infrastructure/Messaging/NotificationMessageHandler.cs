using System.Text.Json;
using Halen.Application.Events;
using Halen.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Halen.Infrastructure.Messaging;

public class NotificationMessageHandler(
    INotificationSender sender,
    ILogger<NotificationMessageHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task HandleAsync(string topic, string message, CancellationToken ct)
    {
        switch (topic)
        {
            case Topics.AppointmentBooked:
                await HandleBooked(message, ct);
                break;

            case Topics.AppointmentCancelled:
                await HandleCancelled(message, ct);
                break;

            case Topics.AppointmentCompleted:
                await HandleCompleted(message, ct);
                break;

            case Topics.PrescriptionIssued:
                await HandlePrescriptionIssued(message, ct);
                break;

            case Topics.PrescriptionCancelled:
                await HandlePrescriptionCancelled(message, ct);
                break;

            default:
                logger.LogWarning("Unknown topic {Topic}, skipping", topic);
                break;
        }
    }

    private async Task HandleBooked(string json, CancellationToken ct)
    {
        var evt = Deserialize<AppointmentBookedEvent>(json, Topics.AppointmentBooked);
        if (evt is null) return;

        var notification = new NotificationDto(
            "appointment.booked",
            $"New appointment with {evt.PatientName} on {evt.ScheduledAt:MMM dd, yyyy 'at' HH:mm}",
            DateTime.UtcNow);

        await sender.SendToUserAsync(evt.DoctorUserId.ToString(), notification, ct);
    }

    private async Task HandleCancelled(string json, CancellationToken ct)
    {
        var evt = Deserialize<AppointmentCancelledEvent>(json, Topics.AppointmentCancelled);
        if (evt is null) return;

        var recipientId = evt.CancelledByUserId == evt.PatientUserId
            ? evt.DoctorUserId
            : evt.PatientUserId;

        var notification = new NotificationDto(
            "appointment.cancelled",
            $"Appointment cancelled by {evt.CancelledByName} ({evt.CancelledByRole})",
            DateTime.UtcNow);

        await sender.SendToUserAsync(recipientId.ToString(), notification, ct);
    }

    private async Task HandleCompleted(string json, CancellationToken ct)
    {
        var evt = Deserialize<AppointmentCompletedEvent>(json, Topics.AppointmentCompleted);
        if (evt is null) return;

        var notification = new NotificationDto(
            "appointment.completed",
            $"Your appointment with Dr. {evt.DoctorName} has been marked as completed",
            DateTime.UtcNow);

        await sender.SendToUserAsync(evt.PatientUserId.ToString(), notification, ct);
    }

    private async Task HandlePrescriptionIssued(string json, CancellationToken ct)
    {
        var evt = Deserialize<PrescriptionIssuedEvent>(json, Topics.PrescriptionIssued);
        if (evt is null) return;

        var notification = new NotificationDto(
            "prescription.issued",
            $"New prescription for {evt.DrugName} from {evt.DoctorName}",
            DateTime.UtcNow);

        await sender.SendToUserAsync(evt.PatientUserId.ToString(), notification, ct);
    }

    private async Task HandlePrescriptionCancelled(string json, CancellationToken ct)
    {
        var evt = Deserialize<PrescriptionCancelledEvent>(json, Topics.PrescriptionCancelled);
        if (evt is null) return;

        var notification = new NotificationDto(
            "prescription.cancelled",
            $"Prescription for {evt.DrugName} has been cancelled by {evt.DoctorName}",
            DateTime.UtcNow);

        await sender.SendToUserAsync(evt.PatientUserId.ToString(), notification, ct);
    }

    private T? Deserialize<T>(string json, string topic)
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (result is null)
                logger.LogWarning("Failed to deserialize message on topic {Topic}: {Json}", topic, json[..Math.Min(json.Length, 500)]);
            return result;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Malformed JSON on topic {Topic}: {Json}", topic, json[..Math.Min(json.Length, 500)]);
            return default;
        }
    }
}
