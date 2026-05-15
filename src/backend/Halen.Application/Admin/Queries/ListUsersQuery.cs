using MediatR;

namespace Halen.Application.Admin.Queries;

public record ListUsersQuery(
    string? Role,
    string? Search,
    bool FlaggedOnly,
    int Page = 1,
    int PageSize = 25
) : IRequest<ListUsersResult>;

public record ListUsersResult(IReadOnlyList<AdminUserDto> Users, int TotalCount);

public record AdminUserDto(
    Guid Id,
    string Name,
    string Role,
    string Status,
    string? Plan,
    DateTime? LastLoginAt,
    bool IsFlagged,
    Guid? DoctorProfileId = null);
