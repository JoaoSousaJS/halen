using MediatR;

namespace Halen.Application.Availability.Queries;

public record GetDoctorAvailabilityQuery(Guid DoctorProfileId) : IRequest<GetDoctorAvailabilityResult>;

public record GetDoctorAvailabilityResult(IReadOnlyList<AvailabilityWindowDto> Windows);

public record AvailabilityWindowDto(Guid Id, string DayOfWeek, string StartTime, string EndTime, int SlotDurationMinutes);
