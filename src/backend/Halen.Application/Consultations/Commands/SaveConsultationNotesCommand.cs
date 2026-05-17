using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Consultations.Commands;

public record SaveConsultationNotesCommand(
    Guid UserId,
    Guid AppointmentId,
    string Notes
) : IRequest<SaveConsultationNotesResult>;

public record SaveConsultationNotesResult(bool Success, string? Error = null, ErrorKind? Kind = null);
