using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Availability.Commands;

public record SetDoctorAvailabilityCommand(
    Guid UserId,
    IReadOnlyList<AvailabilitySlotDto> Slots
) : IRequest<SetDoctorAvailabilityResult>;

public record AvailabilitySlotDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public record SetDoctorAvailabilityResult(bool Success, string? Error = null, ErrorKind? Kind = null);
