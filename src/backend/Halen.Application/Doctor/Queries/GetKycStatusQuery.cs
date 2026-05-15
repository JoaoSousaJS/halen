using Halen.Domain.Enums;
using MediatR;

namespace Halen.Application.Doctor.Queries;

public record GetKycStatusQuery(Guid UserId) : IRequest<GetKycStatusResult>;

public record GetKycStatusResult(
    KycStatus Status,
    DateTime? SubmittedAt,
    string? LastRejectionReason,
    List<KycDocumentDto> Documents);

public record KycDocumentDto(
    Guid Id,
    KycDocumentType DocumentType,
    string FileName,
    DateTime UploadedAt);
