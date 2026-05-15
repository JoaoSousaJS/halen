namespace Halen.Application.Interfaces;

public interface INotificationSender
{
    Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default);
    Task SendToAdminsAsync(NotificationDto notification, CancellationToken ct = default);
}
