using Halen.Application.Attributes;
using Halen.Application.AuditTrail.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = "AdminOnly")]
[RequireFeature("audit_trail")]
public class AuditLogController(IMediator mediator) : HalenControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? actorId,
        [FromQuery] string? action,
        [FromQuery] string? targetId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? clinicId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new SearchAuditLogsQuery(actorId, action, targetId, from, to, clinicId, page, pageSize);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] Guid? actorId,
        [FromQuery] string? action,
        [FromQuery] string? targetId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? clinicId,
        CancellationToken ct = default)
    {
        var query = new ExportAuditLogsCsvQuery(actorId, action, targetId, from, to, clinicId);
        var result = await mediator.Send(query, ct);
        return File(result.CsvBytes, "text/csv", result.FileName);
    }
}
