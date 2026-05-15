using Halen.Application.Admin.Commands;
using Halen.Application.Admin.Queries;
using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(IMediator mediator, IFileStorage fileStorage) : ControllerBase
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

    [HttpGet("doctors/{doctorProfileId:guid}/kyc")]
    public async Task<IActionResult> GetDoctorKycDetails(Guid doctorProfileId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDoctorKycDetailsQuery(doctorProfileId), ct);
        if (!result.Found)
            return NotFound(new { result.Error });

        return Ok(result);
    }

    [HttpPost("doctors/{doctorProfileId:guid}/kyc/review")]
    public async Task<IActionResult> ReviewKyc(Guid doctorProfileId, ReviewKycRequest request, CancellationToken ct)
    {
        var command = new ReviewKycCommand(GetUserId(), doctorProfileId, request.Decision, request.RejectionReason);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { message = "KYC review recorded" });
    }

    [HttpGet("kyc/documents/{documentId:guid}")]
    public async Task<IActionResult> DownloadKycDocument(Guid documentId, CancellationToken ct)
    {
        var doc = await mediator.Send(new GetKycDocumentQuery(documentId), ct);
        if (doc is null)
            return NotFound(new { error = "Document not found" });

        try
        {
            var stream = await fileStorage.ReadAsync(doc.FilePath, ct);
            return File(stream, doc.ContentType, doc.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "File not found on disk" });
        }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")
            ?? throw new UnauthorizedAccessException("Missing 'sub' claim");
        if (!Guid.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("Invalid 'sub' claim");
        return id;
    }

    private IActionResult MapError(string? error, ErrorKind? kind) => kind switch
    {
        ErrorKind.NotFound => NotFound(new { error }),
        ErrorKind.Forbidden => Forbid(),
        _ => BadRequest(new { error }),
    };
}

public record ReviewKycRequest(KycDecision Decision, string? RejectionReason);
