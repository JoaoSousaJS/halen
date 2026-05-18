using MediatR;

namespace Halen.Application.Analytics.Queries;

public record GetDoctorAnalyticsQuery(string Period) : IRequest<DoctorAnalyticsResult>;

public record DoctorAnalyticsResult(
    RankedDoctorDto[] Ranked,
    TopRatedDoctorDto[] TopRated,
    NeedsAttentionDto[] NeedsAttention);
