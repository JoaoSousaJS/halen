using MediatR;

namespace Halen.Application.Doctor.Queries;

public record GetDoctorPatientsQuery(Guid DoctorUserId) : IRequest<IReadOnlyList<DoctorPatientDto>>;

public record DoctorPatientDto(Guid PatientId, string Name);
