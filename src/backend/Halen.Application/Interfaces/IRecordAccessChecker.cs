using Halen.Domain.Enums;

namespace Halen.Application.Interfaces;

public interface IRecordAccessChecker
{
    Task<bool> CanAccessPatientRecord(Guid callerUserId, UserRole callerRole, Guid patientProfileId, CancellationToken ct);
}
