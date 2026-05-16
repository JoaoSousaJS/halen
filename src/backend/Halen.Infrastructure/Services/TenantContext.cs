using Halen.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Halen.Infrastructure.Services;

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid ClinicId => GetClinicId();

    // SECURITY: defaults to true when unauthenticated/no HttpContext so that Identity's
    // UserManager can query users during login (query filters need bypass). Safe because all
    // data endpoints require [Authorize] — unauthenticated requests never reach business logic.
    public bool IsPlatformAdmin
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx is null) return true;
            if (ctx.User.Identity?.IsAuthenticated != true) return true;
            return ctx.User.IsInRole("PlatformAdmin");
        }
    }

    private Guid GetClinicId()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Guid.Empty;

        var claim = ctx.User.FindFirst("clinic_id")?.Value;
        if (claim is null || !Guid.TryParse(claim, out var clinicId))
            throw new UnauthorizedAccessException("Missing clinic_id claim");
        return clinicId;
    }
}
