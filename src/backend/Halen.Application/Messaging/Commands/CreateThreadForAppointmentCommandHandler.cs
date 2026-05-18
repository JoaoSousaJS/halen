using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Commands;

public class CreateThreadForAppointmentCommandHandler(
    IAppDbContext db,
    ILogger<CreateThreadForAppointmentCommandHandler> logger)
    : IRequestHandler<CreateThreadForAppointmentCommand, CreateThreadResult>
{
    public async Task<CreateThreadResult> Handle(CreateThreadForAppointmentCommand request, CancellationToken ct)
    {
        var existing = await db.ConversationThreads
            .FirstOrDefaultAsync(t => t.AppointmentId == request.AppointmentId, ct);

        if (existing is not null)
            return new CreateThreadResult(true, existing.Id);

        var appointment = await db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment is null)
            return new CreateThreadResult(false, Error: "Appointment not found", Kind: ErrorKind.NotFound);

        var specialty = appointment.Doctor.Specialty ?? "Consultation";
        var date = appointment.ScheduledAt.ToString("MMM dd");
        var subject = $"{specialty} consult · {date}";

        var thread = new ConversationThread
        {
            ClinicId = appointment.ClinicId,
            AppointmentId = appointment.Id,
            PatientUserId = appointment.Patient.UserId,
            DoctorUserId = appointment.Doctor.UserId,
            Status = ThreadStatus.Active,
            Subject = subject,
        };

        db.ConversationThreads.Add(thread);

        var systemMessage = new ChatMessage
        {
            ClinicId = appointment.ClinicId,
            ThreadId = thread.Id,
            SenderUserId = appointment.Doctor.UserId,
            MessageType = MessageType.SystemEvent,
            Content = $"Thread created — {subject}",
        };
        db.ChatMessages.Add(systemMessage);

        thread.LastMessageAt = DateTimeOffset.UtcNow;
        thread.LastMessagePreview = systemMessage.Content;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Thread {ThreadId} created for appointment {AppointmentId}",
            thread.Id, appointment.Id);

        return new CreateThreadResult(true, thread.Id);
    }
}
