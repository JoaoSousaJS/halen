using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Reviews.Commands;

public record SubmitReviewCommand(
    Guid PatientUserId,
    Guid AppointmentId,
    int Rating,
    string Title,
    string Body,
    string[] Tags
) : IRequest<SubmitReviewResult>;

public record SubmitReviewResult(
    bool Success,
    Guid? ReviewId = null,
    string? Error = null,
    ErrorKind? Kind = null);
