using Halen.Application.Interfaces;
using MediatR;

namespace Halen.Application.Admin.Commands;

public record CreateDoctorCommand(
    string FirstName,
    string LastName,
    string Email,
    [property: AuditRedact] string Password,
    string Specialty,
    string LicenseNumber,
    decimal ConsultationFee,
    int YearsOfExperience
) : IRequest<CreateDoctorResult>, IAuditableCommand;


public record CreateDoctorResult(bool Success, Guid? DoctorId, string? Error = null);
