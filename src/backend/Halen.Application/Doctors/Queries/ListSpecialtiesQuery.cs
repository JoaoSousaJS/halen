using MediatR;

namespace Halen.Application.Doctors.Queries;

public record ListSpecialtiesQuery() : IRequest<ListSpecialtiesResult>;

public record ListSpecialtiesResult(IReadOnlyList<string> Specialties);
