using Halen.Application.Common;
using Halen.Application.Doctor.Queries;
using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Admin.Queries;

public class GetDoctorKycDetailsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetDoctorKycDetailsQuery, GetDoctorKycDetailsResult>
{
    public async Task<GetDoctorKycDetailsResult> Handle(GetDoctorKycDetailsQuery request, CancellationToken ct)
    {
        var doctor = await db.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.Id == request.DoctorProfileId)
            .Select(d => new
            {
                d.Id,
                DoctorName = d.User.FirstName + " " + d.User.LastName,
                d.Specialty,
                d.LicenseNumber,
                d.KycStatus,
                d.KycSubmittedAt,
            })
            .FirstOrDefaultAsync(ct);

        if (doctor is null)
            return new GetDoctorKycDetailsResult(
                false, Guid.Empty, "", "", "", default, null, [], [],
                "Doctor profile not found.", ErrorKind.NotFound);

        var documents = await db.KycDocuments
            .AsNoTracking()
            .Where(d => d.DoctorProfileId == doctor.Id)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new KycDocumentDto(d.Id, d.DocumentType, d.FileName, d.UploadedAt))
            .ToListAsync(ct);

        var reviews = await db.KycReviews
            .AsNoTracking()
            .Where(r => r.DoctorProfileId == doctor.Id)
            .OrderByDescending(r => r.ReviewedAt)
            .Select(r => new KycReviewDto(
                r.Id,
                r.Decision,
                r.RejectionReason,
                r.ReviewedByUser.FirstName + " " + r.ReviewedByUser.LastName,
                r.ReviewedAt))
            .ToListAsync(ct);

        return new GetDoctorKycDetailsResult(
            true,
            doctor.Id,
            doctor.DoctorName,
            doctor.Specialty,
            doctor.LicenseNumber,
            doctor.KycStatus,
            doctor.KycSubmittedAt,
            documents,
            reviews);
    }
}
