using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record UpdateClinicCommand(Guid ClinicId, string Name, bool IsActive) : IRequest<UpdateClinicResult>;

public record UpdateClinicResult(bool Success, string? Error = null, ErrorKind? Kind = null);
