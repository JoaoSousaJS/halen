using Halen.Application.Consultations.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Halen.API.Hubs;

[Authorize]
public class ConsultationHub(
    IMediator mediator,
    ILogger<ConsultationHub> logger
) : Hub
{
    public async Task JoinRoom(Guid appointmentId)
    {
        var ct = Context.ConnectionAborted;
        var userId = GetUserId();
        var role = GetUserRole();
        var name = GetUserName();

        var result = await mediator.Send(new StartConsultationCommand(userId, appointmentId), ct);
        if (!result.Success)
            throw new HubException(result.Error);

        var joinResult = await mediator.Send(
            new JoinConsultationRoomCommand(userId, appointmentId, role), ct);
        if (!joinResult.Success)
            throw new HubException(joinResult.Error);

        var groupName = $"consultation-{appointmentId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName, ct);

        if (joinResult.ConsultationStarted)
        {
            await Clients.Group(groupName).SendAsync("ConsultationStarted", new
            {
                roomCode = joinResult.RoomCode,
                startedAt = joinResult.StartedAt,
            }, ct);
        }

        await Clients.Group(groupName).SendAsync("ParticipantJoined", new
        {
            name,
            role,
            joinedAt = DateTimeOffset.UtcNow,
        }, ct);

        logger.LogInformation("{Role} {Name} joined consultation room for appointment {AppointmentId}",
            role, name, appointmentId);
    }

    public async Task LeaveRoom(Guid appointmentId)
    {
        var ct = Context.ConnectionAborted;
        var role = GetUserRole();
        var name = GetUserName();

        var groupName = $"consultation-{appointmentId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName, ct);

        await Clients.Group(groupName).SendAsync("ParticipantLeft", new
        {
            name,
            role,
        }, ct);
    }

    public async Task SendChat(Guid appointmentId, string text)
    {
        var ct = Context.ConnectionAborted;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000)
            throw new HubException("Chat message must be between 1 and 1000 characters");

        var role = GetUserRole();
        var name = GetUserName();

        await Clients.Group($"consultation-{appointmentId}").SendAsync("ReceiveChat", new
        {
            from = name,
            role,
            text,
            sentAt = DateTimeOffset.UtcNow,
        }, ct);
    }

    public async Task UpdateNotes(Guid appointmentId, string notes)
    {
        var ct = Context.ConnectionAborted;
        var role = GetUserRole();
        if (role != "Doctor")
            throw new HubException("Only doctors can update notes");

        var userId = GetUserId();
        var result = await mediator.Send(new SaveConsultationNotesCommand(userId, appointmentId, notes), ct);
        if (!result.Success)
            throw new HubException(result.Error);

        await Clients.Caller.SendAsync("NotesUpdated", new { notes }, ct);
    }

    public async Task EndConsultation(Guid appointmentId, string? notes)
    {
        var ct = Context.ConnectionAborted;
        var role = GetUserRole();
        if (role != "Doctor")
            throw new HubException("Only doctors can end consultations");

        var userId = GetUserId();
        var result = await mediator.Send(new EndConsultationCommand(userId, appointmentId, notes), ct);
        if (!result.Success)
            throw new HubException(result.Error);

        await Clients.Group($"consultation-{appointmentId}").SendAsync("ConsultationEnded", new
        {
            endedAt = result.EndedAt,
            notes,
            appointmentId,
        }, ct);
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst("sub")?.Value
            ?? throw new HubException("Missing authentication");
        if (!Guid.TryParse(claim, out var id))
            throw new HubException("Invalid authentication");
        return id;
    }

    private string GetUserRole()
    {
        return Context.User?.FindFirst("role")?.Value
            ?? throw new HubException("Missing role claim");
    }

    private string GetUserName()
    {
        var given = Context.User?.FindFirst("given_name")?.Value ?? "";
        var family = Context.User?.FindFirst("family_name")?.Value ?? "";
        return $"{given} {family}".Trim();
    }
}
