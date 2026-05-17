using MediatR;

namespace Halen.Application.Availability.Queries;

public record GetMyAvailabilityQuery(Guid UserId) : IRequest<GetDoctorAvailabilityResult?>;
