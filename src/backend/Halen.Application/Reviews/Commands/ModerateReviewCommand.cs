using Halen.Application.Common;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Reviews.Commands;

public record ModerateReviewCommand(
    Guid AdminUserId,
    Guid ReviewId,
    ReviewModerationStatus Decision
) : IRequest<ModerateReviewResult>;

public record ModerateReviewResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);
