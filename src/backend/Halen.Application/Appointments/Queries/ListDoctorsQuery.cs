using MediatR;

namespace Halen.Application.Appointments.Queries;

public record ListDoctorsQuery(int Page = 1, int PageSize = 50) : IRequest<ListDoctorsResult>;

public record ListDoctorsResult(IReadOnlyList<DoctorDto> Doctors, int TotalCount);

public record DoctorDto(
    Guid Id,
    string Name,
    string Specialty,
    decimal ConsultationFee,
    int YearsOfExperience
);
