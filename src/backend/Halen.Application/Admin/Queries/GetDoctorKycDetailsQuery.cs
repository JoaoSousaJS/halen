using Halen.Application.Common;
using Halen.Application.Doctor.Queries;
using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Admin.Queries;

public record GetDoctorKycDetailsQuery(Guid DoctorProfileId) : IRequest<GetDoctorKycDetailsResult>;

public record GetDoctorKycDetailsResult(
    bool Found,
    Guid DoctorProfileId,
    string DoctorName,
    string Specialty,
    string LicenseNumber,
    KycStatus Status,
    DateTime? SubmittedAt,
    List<KycDocumentDto> Documents,
    List<KycReviewDto> ReviewHistory,
    string? Error = null,
    ErrorKind? Kind = null);

public record KycReviewDto(
    Guid Id,
    KycDecision Decision,
    string? RejectionReason,
    string ReviewerName,
    DateTime ReviewedAt);
