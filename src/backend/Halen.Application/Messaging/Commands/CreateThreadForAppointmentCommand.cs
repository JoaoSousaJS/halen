using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Messaging.Commands;

public record CreateThreadForAppointmentCommand(
    Guid AppointmentId) : IRequest<CreateThreadResult>;

public record CreateThreadResult(
    bool Success,
    Guid? ThreadId = null,
    string? Error = null,
    ErrorKind? Kind = null);
