namespace Halen.Application.Interfaces;

public interface IAuditableCommand
{
    Guid ActorId => Guid.Empty;
    string AuditAction => GetType().Name.Replace("Command", "");
    string? AuditTargetId => null;
}
