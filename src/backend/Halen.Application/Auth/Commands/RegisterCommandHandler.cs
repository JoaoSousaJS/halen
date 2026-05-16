using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Auth.Commands;

public class RegisterCommandHandler(
    UserManager<User> userManager,
    IJwtService jwtService,
    IAppDbContext db,
    IConfiguration configuration,
    ILogger<RegisterCommandHandler> logger
) : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (request.Role != UserRole.Patient)
            return new RegisterResult(false, null, "Self-registration is only allowed for patients.");

        var defaultSlug = configuration["Seed:DefaultClinicSlug"] ?? "default";
        var defaultClinic = await db.Clinics
            .FirstOrDefaultAsync(c => c.Slug == defaultSlug, ct);

        if (defaultClinic is null)
            return new RegisterResult(false, null, "System configuration error: default clinic not found.");

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            Role = request.Role,
            ClinicId = defaultClinic.Id,
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
            return new RegisterResult(false, null, errors);
        }

        var roleName = request.Role.ToString();
        var roleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            logger.LogError("Role assignment failed for {Email}: {Errors}", request.Email, errors);
            await userManager.DeleteAsync(user);
            return new RegisterResult(false, null, "Account setup failed. Please try again.");
        }

        if (request.Role == UserRole.Doctor)
            user.DoctorProfile = new DoctorProfile { UserId = user.Id, ClinicId = defaultClinic.Id };
        else
            user.PatientProfile = new PatientProfile { UserId = user.Id, ClinicId = defaultClinic.Id };

        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateToken(user, roles);

        logger.LogInformation("User {Email} registered as {Role} in clinic {ClinicSlug}", request.Email, request.Role, defaultSlug);
        return new RegisterResult(true, token);
    }
}
