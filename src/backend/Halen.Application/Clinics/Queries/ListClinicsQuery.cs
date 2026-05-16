using MediatR;

namespace Halen.Application.Clinics.Queries;

public record ListClinicsQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<ListClinicsResult>;

public record ListClinicsResult(List<ClinicDto> Clinics, int TotalCount);

public record ClinicDto(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);
