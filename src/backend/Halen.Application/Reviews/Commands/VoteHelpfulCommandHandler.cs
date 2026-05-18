using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Reviews.Commands;

public class VoteHelpfulCommandHandler(
    IAppDbContext db,
    ILogger<VoteHelpfulCommandHandler> logger
) : IRequestHandler<VoteHelpfulCommand, VoteHelpfulResult>
{
    public async Task<VoteHelpfulResult> Handle(VoteHelpfulCommand request, CancellationToken ct)
    {
        var review = await db.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, ct);

        if (review is null)
            return new VoteHelpfulResult(false, Error: "Review not found.", Kind: ErrorKind.NotFound);

        if (review.ModerationStatus != ReviewModerationStatus.Approved)
            return new VoteHelpfulResult(false, Error: "Review not found.", Kind: ErrorKind.NotFound);

        var alreadyVoted = await db.ReviewHelpfulVotes
            .AnyAsync(v => v.ReviewId == request.ReviewId && v.UserId == request.UserId, ct);

        if (alreadyVoted)
            return new VoteHelpfulResult(false, Error: "You have already voted on this review.", Kind: ErrorKind.Validation);

        db.ReviewHelpfulVotes.Add(new ReviewHelpfulVote
        {
            ClinicId = review.ClinicId,
            ReviewId = request.ReviewId,
            UserId = request.UserId,
        });

        review.HelpfulCount++;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} voted helpful on review {ReviewId}", request.UserId, request.ReviewId);

        return new VoteHelpfulResult(true, NewCount: review.HelpfulCount);
    }
}
