using Halen.Application.Analytics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize(Policy = "PlatformAdmin")]
public class AnalyticsController(IMediator mediator) : HalenControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(
        [FromQuery] string period = "30d", CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAnalyticsOverviewQuery(period), ct));

    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments(
        [FromQuery] string period = "30d", CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAppointmentAnalyticsQuery(period), ct));

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] string period = "30d", CancellationToken ct = default)
        => Ok(await mediator.Send(new GetRevenueAnalyticsQuery(period), ct));

    [HttpGet("heatmap")]
    public async Task<IActionResult> Heatmap(
        [FromQuery] string period = "30d", CancellationToken ct = default)
        => Ok(await mediator.Send(new GetHeatmapAnalyticsQuery(period), ct));

    [HttpGet("doctors")]
    public async Task<IActionResult> Doctors(
        [FromQuery] string period = "30d", CancellationToken ct = default)
        => Ok(await mediator.Send(new GetDoctorAnalyticsQuery(period), ct));

    [HttpGet("geography")]
    public async Task<IActionResult> Geography(
        [FromQuery] string period = "30d", CancellationToken ct = default)
        => Ok(await mediator.Send(new GetGeographyAnalyticsQuery(period), ct));
}
