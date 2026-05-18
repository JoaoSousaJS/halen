using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Reviews.Commands;

public record RespondToReviewCommand(
    Guid DoctorUserId,
    Guid ReviewId,
    string Response
) : IRequest<RespondToReviewResult>;

public record RespondToReviewResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);
