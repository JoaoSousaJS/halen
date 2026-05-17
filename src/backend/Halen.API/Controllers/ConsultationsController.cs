using Halen.Application.Consultations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ConsultationsController(IMediator mediator) : HalenControllerBase
{
    [HttpGet("{appointmentId:guid}")]
    public async Task<IActionResult> Get(Guid appointmentId, CancellationToken ct)
    {
        var query = new GetConsultationRoomQuery(GetUserId(), appointmentId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Room);
    }
}
