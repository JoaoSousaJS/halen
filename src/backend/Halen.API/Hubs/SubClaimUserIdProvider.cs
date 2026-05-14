using Microsoft.AspNetCore.SignalR;

namespace Halen.API.Hubs;

// Depends on MapInboundClaims = false in JWT config — without it, "sub" becomes ClaimTypes.NameIdentifier.
public class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst("sub")?.Value;
}
