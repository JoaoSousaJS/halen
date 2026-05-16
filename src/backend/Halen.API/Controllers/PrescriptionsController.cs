using Halen.Application.Attributes;
using Halen.Application.Prescriptions.Commands;
using Halen.Application.Prescriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[RequireFeature("prescriptions")]
public class PrescriptionsController(IMediator mediator) : HalenControllerBase
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

}

public record IssuePrescriptionRequest(
    Guid PatientId,
    string DrugName,
    string Dosage,
    string Frequency,
    int RefillsRemaining,
    string? PharmacyName);
