using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Profile.Commands;

public class UpdateMyProfileCommandHandler(
    IAppDbContext db
) : IRequestHandler<UpdateMyProfileCommand, UpdateMyProfileResult>
{
    public async Task<UpdateMyProfileResult> Handle(UpdateMyProfileCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null)
            return new UpdateMyProfileResult(false, "User not found", ErrorKind.NotFound);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        // Doctor-specific fields (specialty, fee, etc.) are admin-managed through KYC/creation flows
        if (user.Role == UserRole.Patient)
        {
            var patientProfile = await db.PatientProfiles
                .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

            if (patientProfile is not null)
            {
                if (request.DateOfBirth is not null)
                    patientProfile.DateOfBirth = request.DateOfBirth.Value;

                if (request.City is not null)
                    patientProfile.City = request.City;
            }
        }

        await db.SaveChangesAsync(ct);

        return new UpdateMyProfileResult(true);
    }
}
