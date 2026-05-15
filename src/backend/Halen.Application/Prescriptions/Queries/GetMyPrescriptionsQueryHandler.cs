using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Prescriptions.Queries;

public class GetMyPrescriptionsQueryHandler(
    IAppDbContext db
) : IRequestHandler<GetMyPrescriptionsQuery, GetMyPrescriptionsResult>
{
    public async Task<GetMyPrescriptionsResult> Handle(GetMyPrescriptionsQuery request, CancellationToken ct)
    {
        var query = db.Prescriptions
            .AsNoTracking()
            .AsQueryable();

        query = request.UserRole switch
        {
            UserRole.Doctor => query.Where(p => p.Doctor.UserId == request.UserId),
            UserRole.Patient => query.Where(p => p.Patient.UserId == request.UserId),
            _ => query.Where(_ => false),
        };

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionDto(
                p.Id,
                p.DrugName,
                p.Dosage,
                p.Frequency,
                p.RefillsRemaining,
                p.Status.ToString(),
                p.PharmacyName,
                $"Dr. {p.Doctor.User.LastName}",
                $"{p.Patient.User.FirstName} {p.Patient.User.LastName}",
                p.CreatedAt))
            .ToListAsync(ct);

        return new GetMyPrescriptionsResult(prescriptions);
    }
}
