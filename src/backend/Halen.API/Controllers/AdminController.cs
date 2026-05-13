using Halen.Application.Admin.Commands;
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
}
