using Halen.Application.Attributes;
using Halen.Application.Reviews.Commands;
using Halen.Application.Reviews.Queries;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/reviews")]
[Authorize]
[RequireFeature("doctor_reviews")]
public class ReviewsController(IMediator mediator) : HalenControllerBase
{
    [HttpPost]
    [Authorize(Policy = "PatientOnly")]
    public async Task<IActionResult> Submit(SubmitReviewRequest request, CancellationToken ct)
    {
        var command = new SubmitReviewCommand(
            GetUserId(),
            request.AppointmentId,
            request.Rating,
            request.Title,
            request.Body,
            request.Tags);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Created($"/api/v1/reviews/{result.ReviewId}", new { result.ReviewId });
    }

    [HttpGet("doctor/{doctorProfileId:guid}")]
    public async Task<IActionResult> GetDoctorReviews(
        Guid doctorProfileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "newest",
        CancellationToken ct = default)
    {
        var query = new GetDoctorReviewsQuery(doctorProfileId, page, pageSize, sortBy);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPost("{reviewId:guid}/respond")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Respond(Guid reviewId, RespondToReviewRequest request, CancellationToken ct)
    {
        var command = new RespondToReviewCommand(GetUserId(), reviewId, request.Response);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("{reviewId:guid}/helpful")]
    public async Task<IActionResult> VoteHelpful(Guid reviewId, CancellationToken ct)
    {
        var command = new VoteHelpfulCommand(reviewId, GetUserId());
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { result.NewCount });
    }

    [HttpGet("/api/v1/doctor/reviews")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string filter = "all",
        CancellationToken ct = default)
    {
        var query = new GetMyReviewsQuery(GetUserId(), page, pageSize, filter);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("/api/v1/admin/reviews/moderation")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string filter = "pending",
        CancellationToken ct = default)
    {
        var query = new GetModerationQueueQuery(page, pageSize, filter);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPost("/api/v1/admin/reviews/{reviewId:guid}/moderate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Moderate(Guid reviewId, ModerateReviewRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ReviewModerationStatus>(request.Decision, ignoreCase: true, out var decision))
            return BadRequest(new { error = "Invalid moderation decision." });

        var command = new ModerateReviewCommand(GetUserId(), reviewId, decision);
        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }
}

public record SubmitReviewRequest(Guid AppointmentId, int Rating, string Title, string Body, string[] Tags);
public record RespondToReviewRequest(string Response);
public record ModerateReviewRequest(string Decision);
