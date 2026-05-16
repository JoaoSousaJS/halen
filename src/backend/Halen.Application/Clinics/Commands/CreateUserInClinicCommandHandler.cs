using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Clinics.Commands;

public class CreateUserInClinicCommandHandler(
    UserManager<User> userManager,
    IAppDbContext db,
    ITenantContext tenantContext,
    ILogger<CreateUserInClinicCommandHandler> logger
) : IRequestHandler<CreateUserInClinicCommand, CreateUserInClinicResult>
{
    public async Task<CreateUserInClinicResult> Handle(CreateUserInClinicCommand request, CancellationToken ct)
    {
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            Role = request.Role,
            ClinicId = tenantContext.ClinicId,
            Status = AccountStatus.Active,
        };

        var result = await userManager.CreateAsync(user, request.TemporaryPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("User creation failed for {Email}: {Errors}", request.Email, errors);
            return new CreateUserInClinicResult(false, Error: errors, Kind: ErrorKind.Validation);
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role.ToString());
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            logger.LogWarning("Role assignment failed for {Email}: {Errors}", request.Email, errors);
            return new CreateUserInClinicResult(false, Error: errors, Kind: ErrorKind.Validation);
        }

        if (request.Role == UserRole.Doctor)
        {
            db.DoctorProfiles.Add(new DoctorProfile
            {
                UserId = user.Id,
                ClinicId = tenantContext.ClinicId,
            });
            await db.SaveChangesAsync(ct);
        }
        else if (request.Role == UserRole.Patient)
        {
            db.PatientProfiles.Add(new PatientProfile
            {
                UserId = user.Id,
                ClinicId = tenantContext.ClinicId,
            });
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("User {Email} created in clinic {ClinicId} with role {Role}",
            request.Email, tenantContext.ClinicId, request.Role);

        return new CreateUserInClinicResult(true, user.Id);
    }
}
