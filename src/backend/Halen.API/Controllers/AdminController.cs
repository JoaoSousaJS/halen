using Halen.Application.Admin.Commands;
using Halen.Application.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(IMediator mediator) : ControllerBase
{
    [HttpPost("doctors")]
    public async Task<IActionResult> CreateDoctor(CreateDoctorCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return BadRequest(new { result.Error });

        return Ok(new { result.DoctorId });
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? role,
        [FromQuery] string? search,
        [FromQuery] bool flaggedOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListUsersQuery(role, search, flaggedOnly, page, pageSize), ct);
        return Ok(new { result.Users, result.TotalCount });
    }
}
