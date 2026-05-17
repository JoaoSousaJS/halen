using MediatR;

namespace Halen.Application.Profile.Queries;

public record GetMyProfileQuery(Guid UserId) : IRequest<GetMyProfileResult>;

public record GetMyProfileResult(ProfileDto? Profile);

public record ProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string? Specialty,
    decimal? ConsultationFee,
    int? YearsOfExperience,
    string[]? Languages,
    DateOnly? DateOfBirth,
    string? City,
    string? SubscriptionPlan
);
