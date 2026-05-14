namespace Halen.Application.Interfaces;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
}
