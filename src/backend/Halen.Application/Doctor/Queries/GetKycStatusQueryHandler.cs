using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Doctor.Queries;

public class GetKycStatusQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetKycStatusQuery, GetKycStatusResult>
{
    public async Task<GetKycStatusResult> Handle(GetKycStatusQuery request, CancellationToken ct)
    {
        var doctor = await db.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.UserId == request.UserId)
            .Select(d => new
            {
                d.KycStatus,
                d.KycSubmittedAt,
                d.Id,
            })
            .FirstOrDefaultAsync(ct);

        if (doctor is null)
            return new GetKycStatusResult(KycStatus.NotSubmitted, null, null, []);

        string? rejectionReason = null;
        if (doctor.KycStatus == KycStatus.Rejected)
        {
            rejectionReason = await db.KycReviews
                .AsNoTracking()
                .Where(r => r.DoctorProfileId == doctor.Id && r.Decision == KycDecision.Rejected)
                .OrderByDescending(r => r.ReviewedAt)
                .Select(r => r.RejectionReason)
                .FirstOrDefaultAsync(ct);
        }

        var documents = await db.KycDocuments
            .AsNoTracking()
            .Where(d => d.DoctorProfileId == doctor.Id)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new KycDocumentDto(d.Id, d.DocumentType, d.FileName, d.UploadedAt))
            .ToListAsync(ct);

        return new GetKycStatusResult(doctor.KycStatus, doctor.KycSubmittedAt, rejectionReason, documents);
    }
}
