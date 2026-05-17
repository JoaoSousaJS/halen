using Halen.Application.Profile.Commands;
using Halen.Application.Profile.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileController(IMediator mediator) : HalenControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var query = new GetMyProfileQuery(GetUserId());
        var result = await mediator.Send(query, ct);
        return Ok(new { result.Profile });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateMyProfileCommand(
            GetUserId(),
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.City
        );

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var command = new ChangePasswordCommand(
            GetUserId(),
            request.CurrentPassword,
            request.NewPassword
        );

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }
}

public record UpdateProfileRequest(string FirstName, string LastName, DateOnly? DateOfBirth, string? City);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
