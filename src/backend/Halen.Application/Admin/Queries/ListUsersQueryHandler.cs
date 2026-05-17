using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Admin.Queries;

public class ListUsersQueryHandler(IAppDbContext db)
    : IRequestHandler<ListUsersQuery, ListUsersResult>
{
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromDays(7);

    public async Task<ListUsersResult> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var query = db.Users
            .AsNoTracking()
            .Where(u => u.Role != UserRole.PlatformAdmin)
            .AsQueryable();

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

        if (request.FlaggedOnly)
        {
            query = query.Where(u => u.IsFlagged);
        }

        var ordered = query
            .OrderByDescending(u => u.IsFlagged)
            .ThenByDescending(u => u.LastLoginAt);

        var totalCount = await ordered.CountAsync(ct);

        var users = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new
            {
                u.Id,
                Name = u.FirstName + " " + u.LastName,
                Role = u.Role.ToString(),
                u.Status,
                Plan = u.PatientProfile != null ? u.PatientProfile.SubscriptionPlan : (string?)null,
                u.LastLoginAt,
                u.IsFlagged,
                DoctorProfileId = u.DoctorProfile != null ? (Guid?)u.DoctorProfile.Id : null,
            })
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var dtos = users.Select(u => new AdminUserDto(
            u.Id,
            u.Name,
            u.Role,
            DeriveDisplayStatus(u.Status, u.LastLoginAt, now),
            u.Plan,
            u.LastLoginAt,
            u.IsFlagged,
            u.DoctorProfileId)).ToList();

        return new ListUsersResult(dtos, totalCount);
    }

    private static string DeriveDisplayStatus(AccountStatus status, DateTime? lastLogin, DateTime now)
    {
        if (status != AccountStatus.Active)
            return status.ToString();

        if (lastLogin.HasValue && now - lastLogin.Value > IdleThreshold)
            return "Idle";

        return "Active";
    }
}
