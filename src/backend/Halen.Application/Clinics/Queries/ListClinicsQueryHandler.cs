using Halen.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Clinics.Queries;

public class ListClinicsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListClinicsQuery, ListClinicsResult>
{
    public async Task<ListClinicsResult> Handle(ListClinicsQuery request, CancellationToken ct)
    {
        var query = db.Clinics.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{term}%") || EF.Functions.ILike(c.Slug, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(ct);

        var clinics = await query
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ClinicDto(c.Id, c.Name, c.Slug, c.IsActive, c.CreatedAt))
            .ToListAsync(ct);

        return new ListClinicsResult(clinics, totalCount);
    }
}
