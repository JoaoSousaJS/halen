using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Auth.Commands;

public class RegisterCommandHandler(
    UserManager<User> userManager,
    IJwtService jwtService,
    ILogger<RegisterCommandHandler> logger
) : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            Role = request.Role
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
            return new RegisterResult(false, null, errors);
        }

        var roleName = request.Role.ToString();
        await userManager.AddToRoleAsync(user, roleName);

        if (request.Role == UserRole.Doctor)
            user.DoctorProfile = new DoctorProfile { UserId = user.Id };
        else if (request.Role == UserRole.Patient)
            user.PatientProfile = new PatientProfile { UserId = user.Id };

        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateToken(user, roles);

        logger.LogInformation("User {Email} registered as {Role}", request.Email, request.Role);
        return new RegisterResult(true, token);
    }
}
