using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Auth.Commands;

public class LoginCommandHandler(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IJwtService jwtService,
    IAppDbContext db,
    IAuditContextProvider auditContext,
    ILogger<LoginCommandHandler> logger
) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            await WriteAuditLog("LoginFailure", Guid.Empty, request.Email, Guid.Empty, "Invalid credentials", ct);
            return new LoginResult(false, null, "Invalid credentials");
        }

        if (user.Status == AccountStatus.Suspended)
        {
            await WriteAuditLog("LoginFailure", user.Id, request.Email, user.ClinicId, "Account suspended", ct);
            return new LoginResult(false, null, "Account suspended");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            var reason = result.IsLockedOut ? "Account locked" : "Invalid credentials";
            await WriteAuditLog("LoginFailure", user.Id, request.Email, user.ClinicId, reason, ct);
            return new LoginResult(false, null, reason);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateToken(user, roles);

        await WriteAuditLog("LoginSuccess", user.Id, user.Email!, user.ClinicId, null, ct);
        logger.LogInformation("User {Email} logged in", request.Email);
        return new LoginResult(true, token);
    }

    private async Task WriteAuditLog(string action, Guid actorId, string actorName, Guid clinicId, string? metadata, CancellationToken ct)
    {
        try
        {
            db.AuditLogs.Add(new AuditLog
            {
                Action = action,
                ActorId = actorId,
                ActorName = actorName,
                ClinicId = clinicId,
                Metadata = metadata,
                IpAddress = auditContext.IpAddress,
                TargetId = actorId.ToString()
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write login audit log");
        }
    }
}
