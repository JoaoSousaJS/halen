using Halen.Application.Clinics.Commands;
using Halen.Application.Clinics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "PlatformAdmin")]
public class ClinicsController(IMediator mediator) : HalenControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateClinicRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateClinicCommand(request.Name, request.Slug), ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Created($"/api/v1/clinics/{result.ClinicId}", new { result.ClinicId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateClinicRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateClinicCommand(id, request.Name, request.IsActive), ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListClinicsQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetDetails(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetClinicDetailsQuery(id), ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Clinic);
    }

    [HttpPut("{clinicId:guid}/features/{featureKey}")]
    public async Task<IActionResult> SetFeatureFlag(Guid clinicId, string featureKey, SetFeatureFlagRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SetFeatureFlagCommand(clinicId, featureKey, request.IsEnabled), ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return NoContent();
    }

    [HttpPost("{clinicId:guid}/admins")]
    public async Task<IActionResult> CreateAdmin(Guid clinicId, CreateClinicAdminRequest request, CancellationToken ct)
    {
        var command = new CreateClinicAdminCommand(
            clinicId, request.Email, request.FirstName, request.LastName, request.TemporaryPassword);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Created($"/api/v1/clinics/{clinicId}/admins/{result.UserId}", new { result.UserId });
    }

    [HttpGet("{clinicId:guid}/features")]
    public async Task<IActionResult> GetFeatureFlags(Guid clinicId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetClinicDetailsQuery(clinicId), ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Clinic!.FeatureFlags);
    }

}

public record CreateClinicRequest(string Name, string Slug);
public record UpdateClinicRequest(string Name, bool IsActive);
public record SetFeatureFlagRequest(bool IsEnabled);
public record CreateClinicAdminRequest(string Email, string FirstName, string LastName, string TemporaryPassword);
