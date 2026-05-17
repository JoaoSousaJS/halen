using Halen.Application.Common;
using MediatR;

namespace Halen.Application.Consultations.Queries;

public record GetConsultationRoomQuery(
    Guid UserId,
    Guid AppointmentId
) : IRequest<GetConsultationRoomResult>;

public record GetConsultationRoomResult(bool Success, ConsultationRoomDto? Room = null, string? Error = null, ErrorKind? Kind = null);

public record ConsultationRoomDto(
    Guid Id,
    Guid AppointmentId,
    string RoomCode,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset? DoctorJoinedAt,
    DateTimeOffset? PatientJoinedAt,
    string? Notes,
    string DoctorName,
    string PatientName,
    string Reason,
    int DurationMinutes);
