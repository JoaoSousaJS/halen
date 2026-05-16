using Halen.Application.Interfaces;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Admin.Commands;

public class CreateDoctorCommandHandler(
    UserManager<User> userManager,
    IAppDbContext db,
    ITenantContext tenantContext,
    ILogger<CreateDoctorCommandHandler> logger
) : IRequestHandler<CreateDoctorCommand, CreateDoctorResult>
{
    public async Task<CreateDoctorResult> Handle(CreateDoctorCommand request, CancellationToken ct)
    {
        var user = new User
        {
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Email     = request.Email,
            UserName  = request.Email,
            Role      = UserRole.Doctor,
            Status    = AccountStatus.PendingReview,
            ClinicId  = tenantContext.ClinicId,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Doctor creation failed for {Email}: {Errors}", request.Email, errors);
            return new CreateDoctorResult(false, null, errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "Doctor");
        if (!roleResult.Succeeded)
        {
            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
                logger.LogError("Orphaned user {UserId} ({Email}) — role assignment failed and cleanup delete also failed", user.Id, request.Email);

            return new CreateDoctorResult(false, null, "Role assignment failed. Please try again.");
        }

        var profile = new DoctorProfile
        {
            UserId            = user.Id,
            ClinicId          = tenantContext.ClinicId,
            Specialty         = request.Specialty,
            LicenseNumber     = request.LicenseNumber,
            ConsultationFee   = request.ConsultationFee,
            YearsOfExperience = request.YearsOfExperience,
        };

        db.DoctorProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Doctor account created for {Email} by admin", request.Email);
        return new CreateDoctorResult(true, profile.Id);
    }
}
