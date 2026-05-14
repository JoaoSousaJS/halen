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
    ILogger<LoginCommandHandler> logger
) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return new LoginResult(false, null, "Invalid credentials");

        if (user.Status == AccountStatus.Suspended)
            return new LoginResult(false, null, "Account suspended");

        if (user.Status == AccountStatus.PendingReview)
            return new LoginResult(false, null, "Account is pending review");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return new LoginResult(false, null, result.IsLockedOut ? "Account locked" : "Invalid credentials");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateToken(user, roles);

        logger.LogInformation("User {Email} logged in", request.Email);
        return new LoginResult(true, token);
    }
}
