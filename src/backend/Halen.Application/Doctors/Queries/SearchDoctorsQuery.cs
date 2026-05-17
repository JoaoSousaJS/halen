using MediatR;

namespace Halen.Application.Doctors.Queries;

public record SearchDoctorsQuery(
    string? SearchTerm,
    string? Specialty,
    decimal? MinFee,
    decimal? MaxFee,
    DayOfWeek? AvailableOn,
    string? SortBy,
    int Page = 1,
    int PageSize = 20
) : IRequest<SearchDoctorsResult>;

public record SearchDoctorsResult(IReadOnlyList<DoctorSearchDto> Doctors, int TotalCount);

public record DoctorSearchDto(
    Guid Id,
    string Name,
    string Specialty,
    decimal ConsultationFee,
    int YearsOfExperience,
    string[] Languages,
    NextSlotDto? NextAvailableSlot);

public record NextSlotDto(DateTimeOffset StartUtc, string DayOfWeek);
