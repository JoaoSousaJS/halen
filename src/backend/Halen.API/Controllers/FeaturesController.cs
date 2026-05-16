using Halen.Application.Clinics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/me/features")]
[Authorize]
public class FeaturesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyFeatures(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyFeatureFlagsQuery(), ct);
        return Ok(result);
    }
}
