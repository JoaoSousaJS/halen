using MediatR;

namespace Halen.Application.Clinics.Queries;

public record ListClinicUsersQuery(string? Search = null, string? Role = null, int Page = 1, int PageSize = 20)
    : IRequest<ListClinicUsersResult>;

public record ListClinicUsersResult(List<ClinicUserDto> Users, int TotalCount);

public record ClinicUserDto(Guid Id, string Name, string Email, string Role, string Status, DateTime CreatedAt);
