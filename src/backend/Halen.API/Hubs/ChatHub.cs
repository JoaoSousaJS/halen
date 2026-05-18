using System.Collections.Concurrent;
using Halen.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Halen.API.Hubs;

[Authorize]
public class ChatHub(
    IServiceScopeFactory scopeFactory,
    ILogger<ChatHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<(Guid ThreadId, Guid UserId), DateTimeOffset> LastTyping = new();

    public async Task JoinThread(Guid threadId)
    {
        var userId = GetUserId();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var thread = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.Id == threadId, Context.ConnectionAborted);

        if (thread is null)
            throw new HubException("Thread not found");

        if (thread.PatientUserId != userId && thread.DoctorUserId != userId)
            throw new HubException("You are not a participant in this thread");

        var groupName = $"chat-{thread.ClinicId}-{threadId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        logger.LogInformation("User {UserId} joined chat thread {ThreadId}", userId, threadId);
    }

    public async Task LeaveThread(Guid threadId)
    {
        var userId = GetUserId();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var thread = await db.ConversationThreads
            .Select(t => new { t.Id, t.ClinicId })
            .FirstOrDefaultAsync(t => t.Id == threadId, Context.ConnectionAborted);

        if (thread is null) return;

        var groupName = $"chat-{thread.ClinicId}-{threadId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendTyping(Guid threadId)
    {
        var userId = GetUserId();
        var key = (threadId, userId);
        var now = DateTimeOffset.UtcNow;

        if (LastTyping.TryGetValue(key, out var lastTime) && (now - lastTime).TotalSeconds < 3)
            return;

        LastTyping[key] = now;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var thread = await db.ConversationThreads
            .Select(t => new { t.Id, t.ClinicId, t.PatientUserId, t.DoctorUserId })
            .FirstOrDefaultAsync(t => t.Id == threadId, Context.ConnectionAborted);

        if (thread is null || (thread.PatientUserId != userId && thread.DoctorUserId != userId))
            return;

        var userName = GetUserName();
        var groupName = $"chat-{thread.ClinicId}-{threadId}";

        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", threadId, userName);
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst("sub")?.Value
            ?? throw new HubException("Missing user claim");
        return Guid.Parse(sub);
    }

    private string GetUserName()
    {
        var first = Context.User?.FindFirst("given_name")?.Value ?? "";
        var last = Context.User?.FindFirst("family_name")?.Value ?? "";
        return $"{first} {last}".Trim();
    }
}
