using Halen.Application.Attributes;
using Halen.Application.Messaging.Commands;
using Halen.Application.Messaging.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/messaging")]
[Authorize]
[RequireFeature("messaging")]
public class ChatController(IMediator mediator) : HalenControllerBase
{
    [HttpGet("threads")]
    public async Task<IActionResult> GetMyThreads(
        [FromQuery] string? filter,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new GetMyThreadsQuery(
            GetUserId(), GetUserRoleEnum(), filter, search, page, pageSize);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { result.Threads, result.TotalCount });
    }

    [HttpGet("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> GetThreadMessages(
        Guid threadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new GetThreadMessagesQuery(GetUserId(), threadId, page, pageSize);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { result.Messages, result.TotalCount });
    }

    [HttpPost("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid threadId, SendMessageRequest request, CancellationToken ct)
    {
        var command = new SendMessageCommand(GetUserId(), threadId, request.Content);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetThreadMessages),
            new { threadId }, new { result.MessageId });
    }

    [HttpPost("threads/{threadId:guid}/attachments")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> SendAttachment(
        Guid threadId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File is required" });

        var command = new SendAttachmentCommand(
            GetUserId(), threadId,
            file.FileName, file.ContentType, file.Length, file.OpenReadStream());

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetThreadMessages),
            new { threadId }, new { result.MessageId });
    }

    [HttpPost("threads/{threadId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid threadId, CancellationToken ct)
    {
        var command = new MarkMessagesReadCommand(GetUserId(), threadId);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("threads/{threadId:guid}/close")]
    public async Task<IActionResult> CloseThread(Guid threadId, CancellationToken ct)
    {
        var command = new CloseThreadCommand(GetUserId(), threadId);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchMessages(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Search query must be at least 2 characters" });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new SearchMessagesQuery(GetUserId(), q, page, pageSize);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { result.Hits, result.TotalCount });
    }

    [HttpGet("threads/{threadId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(
        Guid threadId, Guid attachmentId, CancellationToken ct)
    {
        var query = new DownloadAttachmentQuery(GetUserId(), threadId, attachmentId);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(result.FileStream!, result.ContentType!, result.FileName!);
    }
}

public record SendMessageRequest(string Content);
