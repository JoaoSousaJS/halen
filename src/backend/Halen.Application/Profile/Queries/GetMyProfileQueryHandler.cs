using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Profile.Queries;

public class GetMyProfileQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetMyProfileQuery, GetMyProfileResult>
{
    public async Task<GetMyProfileResult> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var profile = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new ProfileDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email!,
                u.Role.ToString(),
                u.CreatedAt,
                u.LastLoginAt,
                u.Role == UserRole.Doctor ? u.DoctorProfile!.Specialty : null,
                u.Role == UserRole.Doctor ? u.DoctorProfile!.ConsultationFee : null,
                u.Role == UserRole.Doctor ? u.DoctorProfile!.YearsOfExperience : null,
                u.Role == UserRole.Doctor ? u.DoctorProfile!.Languages : null,
                u.Role == UserRole.Patient ? u.PatientProfile!.DateOfBirth : null,
                u.Role == UserRole.Patient ? u.PatientProfile!.City : null,
                u.Role == UserRole.Patient ? u.PatientProfile!.SubscriptionPlan : null
            ))
            .FirstOrDefaultAsync(ct);

        return new GetMyProfileResult(profile);
    }
}
