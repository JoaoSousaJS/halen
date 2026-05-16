using Halen.Application.Attributes;
using Halen.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Halen.API.Middleware;

public class FeatureFlagMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<RequireFeatureAttribute>();

        if (attribute is null)
        {
            await next(context);
            return;
        }

        if (context.User.IsInRole("PlatformAdmin"))
        {
            await next(context);
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        var db = context.RequestServices.GetRequiredService<IAppDbContext>();

        var isEnabled = await db.ClinicFeatureFlags
            .AsNoTracking()
            .AnyAsync(f => f.ClinicId == tenantContext.ClinicId
                        && f.FeatureKey == attribute.FeatureKey
                        && f.IsEnabled, context.RequestAborted);

        if (!isEnabled)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Feature not available for your clinic" });
            return;
        }

        await next(context);
    }
}
