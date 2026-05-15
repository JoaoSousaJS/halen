using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Prescriptions.Commands;

public record CancelPrescriptionCommand(
    Guid DoctorUserId,
    Guid PrescriptionId
) : IRequest<CancelPrescriptionResult>;

public record CancelPrescriptionResult(
    bool Success,
    string? Error = null,
    ErrorKind? Kind = null);
