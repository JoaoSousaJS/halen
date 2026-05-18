using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Reviews.Commands;

public class VoteHelpfulCommandHandler(
    IAppDbContext db
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

        review.HelpfulCount++;

        await db.SaveChangesAsync(ct);

        return new VoteHelpfulResult(true, NewCount: review.HelpfulCount);
    }
}
