using Halen.Application.Doctors.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DoctorsController(IMediator mediator) : HalenControllerBase
{
    [HttpGet("search")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? specialty,
        [FromQuery] decimal? minFee,
        [FromQuery] decimal? maxFee,
        [FromQuery] DayOfWeek? availableOn,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new SearchDoctorsQuery(
            search, specialty, minFee, maxFee, availableOn, sortBy, page, pageSize);

        var result = await mediator.Send(query, ct);

        return Ok(result);
    }

    [HttpGet("specialties")]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> Specialties(CancellationToken ct)
    {
        var result = await mediator.Send(new ListSpecialtiesQuery(), ct);

        return Ok(result);
    }
}
