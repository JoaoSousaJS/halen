using Halen.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Halen.API.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    public const string AdminGroup = "Admins";

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst("role")?.Value;
        if (role == "Admin")
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

        await base.OnConnectedAsync();
    }
}
