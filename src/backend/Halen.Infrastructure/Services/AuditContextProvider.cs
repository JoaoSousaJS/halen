using Halen.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Halen.Infrastructure.Services;

public class AuditContextProvider(IHttpContextAccessor httpContextAccessor) : IAuditContextProvider
{
    public Guid ActorId => ExtractActorId();
    public string ActorName => ExtractActorName();
    public string IpAddress => ExtractIpAddress();

    private Guid ExtractActorId()
    {
        var ctx = httpContextAccessor.HttpContext;
        var sub = ctx?.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string ExtractActorName()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx?.User is null) return "Unknown";
        return ctx.User.FindFirst("given_name")?.Value is { } given
            && ctx.User.FindFirst("family_name")?.Value is { } family
            ? $"{given} {family}"
            : ctx.User.FindFirst("name")?.Value
              ?? ctx.User.FindFirst("sub")?.Value
              ?? "Unknown";
    }

    private string ExtractIpAddress()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return "Unknown";
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
