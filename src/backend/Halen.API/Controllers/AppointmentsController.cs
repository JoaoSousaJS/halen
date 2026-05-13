using Halen.Application.Appointments.Commands;
using Halen.Application.Appointments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AppointmentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> Book(BookAppointmentRequest request, CancellationToken ct)
    {
        var command = new BookAppointmentCommand(
            GetUserId(),
            request.DoctorId,
            request.ScheduledAt,
            request.Reason
        );

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return BadRequest(new { result.Error });

        return Ok(new { result.AppointmentId });
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var query = new GetMyAppointmentsQuery(GetUserId(), GetUserRole());
        var result = await mediator.Send(query, ct);
        return Ok(result.Appointments);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var command = new CancelAppointmentCommand(GetUserId(), GetUserRole(), id);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return BadRequest(new { result.Error });

        return Ok();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Complete(Guid id, CompleteAppointmentRequest request, CancellationToken ct)
    {
        var command = new CompleteAppointmentCommand(GetUserId(), id, request.Notes);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return BadRequest(new { result.Error });

        return Ok();
    }

    [HttpGet("doctors")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> ListDoctors(CancellationToken ct)
    {
        var result = await mediator.Send(new ListDoctorsQuery(), ct);
        return Ok(result.Doctors);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst("sub")!.Value);

    private string GetUserRole() =>
        User.FindFirst("role")!.Value;
}

public record BookAppointmentRequest(Guid DoctorId, DateTime ScheduledAt, string Reason);

public record CompleteAppointmentRequest(string? Notes);
