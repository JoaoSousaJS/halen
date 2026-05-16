using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Clinics.Commands;

public record CreateClinicCommand(string Name, string Slug) : IRequest<CreateClinicResult>;

public record CreateClinicResult(bool Success, Guid? ClinicId = null, string? Error = null, ErrorKind? Kind = null);
