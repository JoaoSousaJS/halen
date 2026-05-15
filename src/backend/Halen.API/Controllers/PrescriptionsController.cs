using Halen.Application.Common;
using Halen.Application.Prescriptions.Commands;
using Halen.Application.Prescriptions.Queries;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PrescriptionsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Issue(IssuePrescriptionRequest request, CancellationToken ct)
    {
        var command = new IssuePrescriptionCommand(
            GetUserId(),
            request.PatientId,
            request.DrugName,
            request.Dosage,
            request.Frequency,
            request.RefillsRemaining,
            request.PharmacyName);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetMine), new { result.PrescriptionId });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var command = new CancelPrescriptionCommand(GetUserId(), id);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var query = new GetMyPrescriptionsQuery(GetUserId(), GetUserRoleEnum());
        var result = await mediator.Send(query, ct);
        return Ok(result.Prescriptions);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")
            ?? throw new UnauthorizedAccessException("Missing 'sub' claim");
        if (!Guid.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("Invalid 'sub' claim");
        return id;
    }

    private UserRole GetUserRoleEnum()
    {
        var claim = User.FindFirst("role")
            ?? throw new UnauthorizedAccessException("Missing 'role' claim");
        if (!Enum.TryParse<UserRole>(claim.Value, out var parsed))
            throw new UnauthorizedAccessException($"Unrecognized role '{claim.Value}'");
        return parsed;
    }

    private IActionResult MapError(string? error, ErrorKind? kind) => kind switch
    {
        ErrorKind.NotFound => NotFound(new { error }),
        ErrorKind.Forbidden => Forbid(),
        _ => BadRequest(new { error }),
    };
}

public record IssuePrescriptionRequest(
    Guid PatientId,
    string DrugName,
    string Dosage,
    string Frequency,
    int RefillsRemaining,
    string? PharmacyName);
