using Halen.Application.Appointments.Commands;
using Halen.Application.Appointments.Queries;
using Halen.Application.Common;
using Halen.Domain.Enums;
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

        return CreatedAtAction(nameof(GetMine), new { result.AppointmentId });
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var query = new GetMyAppointmentsQuery(GetUserId(), GetUserRoleEnum());
        var result = await mediator.Send(query, ct);
        return Ok(result.Appointments);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var command = new CancelAppointmentCommand(GetUserId(), GetUserRoleEnum(), id);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Complete(Guid id, CompleteAppointmentRequest request, CancellationToken ct)
    {
        var command = new CompleteAppointmentCommand(GetUserId(), id, request.Notes);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpGet("doctors")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> ListDoctors(CancellationToken ct)
    {
        var result = await mediator.Send(new ListDoctorsQuery(), ct);
        return Ok(result.Doctors);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")
            ?? throw new UnauthorizedAccessException("Missing 'sub' claim");
        return Guid.Parse(claim.Value);
    }

    private string GetUserRole()
    {
        var claim = User.FindFirst("role")
            ?? throw new UnauthorizedAccessException("Missing 'role' claim");
        return claim.Value;
    }

    private UserRole GetUserRoleEnum()
    {
        var role = GetUserRole();
        if (!Enum.TryParse<UserRole>(role, out var parsed))
            throw new UnauthorizedAccessException($"Unrecognized role '{role}'");
        return parsed;
    }

    private IActionResult MapError(string? error, ErrorKind? kind) => kind switch
    {
        ErrorKind.NotFound => NotFound(new { error }),
        ErrorKind.Forbidden => Forbid(),
        _ => BadRequest(new { error }),
    };
}

public record BookAppointmentRequest(Guid DoctorId, DateTime ScheduledAt, string Reason);

public record CompleteAppointmentRequest(string? Notes);
