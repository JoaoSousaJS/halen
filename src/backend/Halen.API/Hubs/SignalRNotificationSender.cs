using Halen.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Halen.API.Hubs;

public class SignalRNotificationSender(
    IHubContext<NotificationHub, INotificationClient> hubContext
) : INotificationSender
{
    public async Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default)
    {
        await hubContext.Clients.User(userId).ReceiveNotification(notification);
    }

    public async Task SendToAdminsAsync(NotificationDto notification, CancellationToken ct = default)
    {
        await hubContext.Clients.Group(NotificationHub.AdminGroup).ReceiveNotification(notification);
    }
}
