using Halen.Application.Availability.Commands;
using Halen.Application.Availability.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/availability")]
[Authorize]
public class AvailabilityController(IMediator mediator) : HalenControllerBase
{
    [HttpPut("mine")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> SetMyAvailability(SetAvailabilityRequest request, CancellationToken ct)
    {
        var slots = new List<AvailabilitySlotDto>();
        foreach (var s in request.Slots)
        {
            if (!TimeOnly.TryParse(s.StartTime, out var start) || !TimeOnly.TryParse(s.EndTime, out var end))
                return BadRequest(new { error = $"Invalid time format: '{s.StartTime}' or '{s.EndTime}'. Use HH:mm." });
            slots.Add(new AvailabilitySlotDto(s.DayOfWeek, start, end));
        }

        var command = new SetDoctorAvailabilityCommand(GetUserId(), slots);
        var result = await mediator.Send(command, ct);

        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { message = "Availability updated successfully." });
    }

    [HttpGet("mine")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> GetMyAvailability(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyAvailabilityQuery(GetUserId()), ct);
        if (result is null)
            return NotFound(new { error = "Doctor profile not found." });
        return Ok(result);
    }

    [HttpGet("{doctorId:guid}")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> GetDoctorAvailability(Guid doctorId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDoctorAvailabilityQuery(doctorId), ct);
        return Ok(result);
    }

    [HttpGet("{doctorId:guid}/slots")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> GetAvailableSlots(Guid doctorId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAvailableSlotsQuery(doctorId, date), ct);
        return Ok(result);
    }
}

public record SetAvailabilitySlotRequest(DayOfWeek DayOfWeek, string StartTime, string EndTime);

public record SetAvailabilityRequest(List<SetAvailabilitySlotRequest> Slots);
