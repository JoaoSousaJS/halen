using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Clinics.Commands;

public class CreateClinicAdminCommandHandler(
    UserManager<User> userManager,
    IAppDbContext db,
    ILogger<CreateClinicAdminCommandHandler> logger
) : IRequestHandler<CreateClinicAdminCommand, CreateClinicAdminResult>
{
    public async Task<CreateClinicAdminResult> Handle(CreateClinicAdminCommand request, CancellationToken ct)
    {
        var clinic = await db.Clinics.FirstOrDefaultAsync(c => c.Id == request.ClinicId, ct);
        if (clinic is null)
            return new CreateClinicAdminResult(false, Error: "Clinic not found", Kind: ErrorKind.NotFound);

        if (!clinic.IsActive)
            return new CreateClinicAdminResult(false, Error: "Cannot create admin for an inactive clinic", Kind: ErrorKind.Validation);

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            Role = UserRole.ClinicAdmin,
            ClinicId = request.ClinicId,
            Status = AccountStatus.Active,
        };

        var result = await userManager.CreateAsync(user, request.TemporaryPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("ClinicAdmin creation failed for {Email}: {Errors}", request.Email, errors);
            return new CreateClinicAdminResult(false, Error: errors, Kind: ErrorKind.Validation);
        }

        var roleResult = await userManager.AddToRoleAsync(user, UserRole.ClinicAdmin.ToString());
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            logger.LogWarning("ClinicAdmin role assignment failed for {Email}: {Errors}", request.Email, errors);
            return new CreateClinicAdminResult(false, Error: errors, Kind: ErrorKind.Validation);
        }

        logger.LogInformation("ClinicAdmin {Email} created for clinic {ClinicId}", request.Email, request.ClinicId);
        return new CreateClinicAdminResult(true, user.Id);
    }
}
