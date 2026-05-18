using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Reviews.Commands;

public record VoteHelpfulCommand(
    Guid ReviewId
) : IRequest<VoteHelpfulResult>;

public record VoteHelpfulResult(
    bool Success,
    int? NewCount = null,
    string? Error = null,
    ErrorKind? Kind = null);
