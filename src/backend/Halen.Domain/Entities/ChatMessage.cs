using Halen.Domain.Enums;
using Halen.Domain.Interfaces;

namespace Halen.Domain.Entities;

public class ChatMessage : BaseEntity, ITenantScoped
{
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid ThreadId { get; set; }
    public ConversationThread? Thread { get; set; }

    public Guid SenderUserId { get; set; }
    public User? SenderUser { get; set; }

    public MessageType MessageType { get; set; }
    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public List<MessageAttachment> Attachments { get; set; } = [];
}
