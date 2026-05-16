using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class KycDocument : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public KycDocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
