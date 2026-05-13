using MediatR;

namespace Halen.Application.Appointments.Queries;

public record ListDoctorsQuery : IRequest<ListDoctorsResult>;

public record ListDoctorsResult(IReadOnlyList<DoctorDto> Doctors);

public record DoctorDto(
    Guid Id,
    string Name,
    string Specialty,
    decimal ConsultationFee,
    int YearsOfExperience
);
