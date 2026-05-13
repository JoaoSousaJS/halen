using Halen.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return BadRequest(new { result.Error });

        return Ok(new { result.Token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return Unauthorized(new { result.Error });

        return Ok(new { result.Token });
    }
}
