using Halen.Application.Clinics.Commands;
using Halen.Application.Clinics.Queries;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/clinic/users")]
[Authorize(Policy = "ClinicAdmin")]
public class ClinicUsersController(IMediator mediator) : HalenControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserInClinicRequest request, CancellationToken ct)
    {
        var command = new CreateUserInClinicCommand(
            request.Email, request.FirstName, request.LastName,
            request.TemporaryPassword, request.Role);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Created($"/api/v1/clinic/users/{result.UserId}", new { result.UserId });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListClinicUsersQuery(search, role, page, pageSize), ct);
        return Ok(result);
    }

}

public record CreateUserInClinicRequest(string Email, string FirstName, string LastName, string TemporaryPassword, UserRole Role);
