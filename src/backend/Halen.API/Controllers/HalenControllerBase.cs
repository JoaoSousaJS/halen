using Halen.Application.Common;
using Halen.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

public abstract class HalenControllerBase : ControllerBase
{
    protected Guid GetUserId()
    {
        var claim = User.FindFirst("sub")
            ?? throw new UnauthorizedAccessException("Missing 'sub' claim");
        if (!Guid.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("Invalid 'sub' claim");
        return id;
    }

    protected UserRole GetUserRoleEnum()
    {
        var claim = User.FindFirst("role")
            ?? throw new UnauthorizedAccessException("Missing 'role' claim");
        if (!Enum.TryParse<UserRole>(claim.Value, out var parsed))
            throw new UnauthorizedAccessException($"Unrecognized role '{claim.Value}'");
        return parsed;
    }

    protected IActionResult MapError(string? error, ErrorKind? kind) => kind switch
    {
        ErrorKind.NotFound => NotFound(new { error }),
        ErrorKind.Forbidden => Forbid(),
        _ => BadRequest(new { error }),
    };
}
