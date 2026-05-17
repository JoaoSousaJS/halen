using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Profile.Commands;

public record UpdateMyProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? City
) : IRequest<UpdateMyProfileResult>;

public record UpdateMyProfileResult(bool Success, string? Error = null, ErrorKind? Kind = null);
