using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class MessageAttachment : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid MessageId { get; set; }
    public ChatMessage? Message { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public MessageAttachmentType AttachmentType { get; set; }
}
