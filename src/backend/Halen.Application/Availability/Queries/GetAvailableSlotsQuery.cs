using MediatR;

namespace Halen.Application.Availability.Queries;

public record GetAvailableSlotsQuery(Guid DoctorProfileId, DateOnly Date) : IRequest<GetAvailableSlotsResult>;

public record GetAvailableSlotsResult(IReadOnlyList<TimeSlotDto> Slots);

public record TimeSlotDto(DateTime StartUtc, string StartLocal, bool IsAvailable);
