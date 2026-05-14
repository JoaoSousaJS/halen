using Halen.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Halen.API.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>;
