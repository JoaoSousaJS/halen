using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Clinics.Commands;

public class UpdateClinicCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateClinicCommand, UpdateClinicResult>
{
    public async Task<UpdateClinicResult> Handle(UpdateClinicCommand request, CancellationToken ct)
    {
        var clinic = await db.Clinics.FindAsync([request.ClinicId], ct);

        if (clinic is null)
            return new UpdateClinicResult(false, "Clinic not found", ErrorKind.NotFound);

        clinic.Name = request.Name;

        if (!request.IsActive && clinic.IsActive)
        {
            await db.Users
                .Where(u => u.ClinicId == clinic.Id && u.Status != AccountStatus.Suspended)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, AccountStatus.Suspended), ct);
        }

        clinic.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return new UpdateClinicResult(true);
    }
}
