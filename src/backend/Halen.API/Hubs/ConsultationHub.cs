using Halen.Application.Consultations.Commands;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Halen.API.Hubs;

[Authorize]
public class ConsultationHub(
    IMediator mediator,
    IAppDbContext db,
    ILogger<ConsultationHub> logger
) : Hub
{
    public async Task JoinRoom(Guid appointmentId)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var result = await mediator.Send(new StartConsultationCommand(userId, appointmentId));
        if (!result.Success)
            throw new HubException(result.Error);

        var groupName = $"consultation-{appointmentId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var room = await db.ConsultationRooms
            .Include(r => r.Appointment)
            .FirstAsync(r => r.AppointmentId == appointmentId);

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        var name = $"{user.FirstName} {user.LastName}";

        if (role == "Doctor")
            room.DoctorJoinedAt = DateTimeOffset.UtcNow;
        else
            room.PatientJoinedAt = DateTimeOffset.UtcNow;

        if (room.DoctorJoinedAt is not null && room.PatientJoinedAt is not null
            && room.Status == ConsultationRoomStatus.Waiting)
        {
            room.Status = ConsultationRoomStatus.Active;
            room.StartedAt = DateTimeOffset.UtcNow;
            room.Appointment!.Status = AppointmentStatus.InProgress;

            await db.SaveChangesAsync();

            await Clients.Group(groupName).SendAsync("ConsultationStarted", new
            {
                roomCode = room.RoomCode,
                startedAt = room.StartedAt,
            });
        }
        else
        {
            await db.SaveChangesAsync();
        }

        await Clients.Group(groupName).SendAsync("ParticipantJoined", new
        {
            name,
            role,
            joinedAt = DateTimeOffset.UtcNow,
        });

        logger.LogInformation("{Role} {Name} joined consultation room for appointment {AppointmentId}",
            role, name, appointmentId);
    }

    public async Task LeaveRoom(Guid appointmentId)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var room = await db.ConsultationRooms
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);

        if (room is not null)
        {
            if (role == "Doctor")
                room.DoctorJoinedAt = null;
            else
                room.PatientJoinedAt = null;

            await db.SaveChangesAsync();
        }

        var groupName = $"consultation-{appointmentId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        await Clients.Group(groupName).SendAsync("ParticipantLeft", new
        {
            name = $"{user.FirstName} {user.LastName}",
            role,
        });
    }

    public async Task SendChat(Guid appointmentId, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000)
            throw new HubException("Chat message must be between 1 and 1000 characters");

        var userId = GetUserId();
        var role = GetUserRole();
        var user = await db.Users.FirstAsync(u => u.Id == userId);

        await Clients.Group($"consultation-{appointmentId}").SendAsync("ReceiveChat", new
        {
            from = $"{user.FirstName} {user.LastName}",
            role,
            text,
            sentAt = DateTimeOffset.UtcNow,
        });
    }

    public async Task UpdateNotes(Guid appointmentId, string notes)
    {
        var role = GetUserRole();
        if (role != "Doctor")
            throw new HubException("Only doctors can update notes");

        var userId = GetUserId();
        var result = await mediator.Send(new SaveConsultationNotesCommand(userId, appointmentId, notes));
        if (!result.Success)
            throw new HubException(result.Error);

        await Clients.Caller.SendAsync("NotesUpdated", new { notes });
    }

    public async Task EndConsultation(Guid appointmentId, string? notes)
    {
        var role = GetUserRole();
        if (role != "Doctor")
            throw new HubException("Only doctors can end consultations");

        var userId = GetUserId();
        var result = await mediator.Send(new EndConsultationCommand(userId, appointmentId, notes));
        if (!result.Success)
            throw new HubException(result.Error);

        await Clients.Group($"consultation-{appointmentId}").SendAsync("ConsultationEnded", new
        {
            endedAt = result.EndedAt,
            notes,
            appointmentId,
        });
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
}
