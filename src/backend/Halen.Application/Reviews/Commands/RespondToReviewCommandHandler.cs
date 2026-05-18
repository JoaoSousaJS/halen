using Halen.Application.Common;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Reviews.Commands;

public class RespondToReviewCommandHandler(
    IAppDbContext db
) : IRequestHandler<RespondToReviewCommand, RespondToReviewResult>
{
    public async Task<RespondToReviewResult> Handle(RespondToReviewCommand request, CancellationToken ct)
    {
        var doctorProfile = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == request.DoctorUserId, ct);

        if (doctorProfile is null)
            return new RespondToReviewResult(false, Error: "Doctor profile not found.", Kind: ErrorKind.NotFound);

        var review = await db.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, ct);

        if (review is null)
            return new RespondToReviewResult(false, Error: "Review not found.", Kind: ErrorKind.NotFound);

        if (review.DoctorProfileId != doctorProfile.Id)
            return new RespondToReviewResult(false, Error: "You can only respond to reviews for your own profile.", Kind: ErrorKind.Forbidden);

        if (review.DoctorResponse is not null)
            return new RespondToReviewResult(false, Error: "A response has already been posted.", Kind: ErrorKind.Validation);

        review.DoctorResponse = request.Response;
        review.DoctorRespondedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new RespondToReviewResult(true);
    }
}
