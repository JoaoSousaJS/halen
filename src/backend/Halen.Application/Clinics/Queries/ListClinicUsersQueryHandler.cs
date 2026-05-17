using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Clinics.Queries;

public class ListClinicUsersQueryHandler(IAppDbContext db)
    : IRequestHandler<ListClinicUsersQuery, ListClinicUsersResult>
{
    public async Task<ListClinicUsersResult> Handle(ListClinicUsersQuery request, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                EF.Functions.ILike(u.FirstName, $"%{term}%") ||
                EF.Functions.ILike(u.LastName, $"%{term}%") ||
                (u.Email != null && EF.Functions.ILike(u.Email, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new ClinicUserDto(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.Email!,
                u.Role.ToString(),
                u.Status.ToString(),
                u.CreatedAt))
            .ToListAsync(ct);

        return new ListClinicUsersResult(users, totalCount);
    }
}
