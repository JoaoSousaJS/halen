using Halen.Application.Attributes;
using Halen.Application.MedicalRecords.Commands;
using Halen.Application.MedicalRecords.Queries;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/record-access")]
[Authorize(Policy = "PlatformAdmin")]
[RequireFeature("medical_records")]
public class RecordAccessController(IMediator mediator) : HalenControllerBase
{
    [HttpGet("{patientProfileId:guid}/matrix")]
    public async Task<IActionResult> GetAccessMatrix(
        Guid patientProfileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetRecordAccessMatrixQuery(
            GetUserId(), GetUserRoleEnum(), patientProfileId, page, pageSize);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { result.Entries, result.TotalCount });
    }

    [HttpGet("{patientProfileId:guid}/logs")]
    public async Task<IActionResult> GetAccessLogs(
        Guid patientProfileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetRecordAccessLogsQuery(
            GetUserId(), GetUserRoleEnum(), patientProfileId, page, pageSize);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { result.Logs, result.TotalCount });
    }

    [HttpPost("{patientProfileId:guid}/grant")]
    public async Task<IActionResult> GrantAccess(
        Guid patientProfileId, GrantRecordAccessRequest request, CancellationToken ct)
    {
        var command = new GrantRecordAccessCommand(
            GetUserId(), patientProfileId,
            request.GrantToUserId, request.AccessLevel, request.Reason);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetAccessMatrix),
            new { patientProfileId }, new { result.AccessId });
    }

    [HttpPost("{accessId:guid}/revoke")]
    public async Task<IActionResult> RevokeAccess(
        Guid accessId, RevokeRecordAccessRequest? request, CancellationToken ct)
    {
        var command = new RevokeRecordAccessCommand(
            GetUserId(), accessId, request?.Reason);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }
}

public record GrantRecordAccessRequest(
    Guid GrantToUserId,
    RecordAccessLevel AccessLevel,
    string? Reason);

public record RevokeRecordAccessRequest(
    string? Reason);
