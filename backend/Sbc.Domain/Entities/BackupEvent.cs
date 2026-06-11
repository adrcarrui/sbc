using Sbc.Domain.Common;
using Sbc.Domain.Enums;

namespace Sbc.Domain.Entities;

public class BackupEvent : AuditableEntity
{
    public Guid? ProtectedSystemId { get; set; }

    public ProtectedSystem? ProtectedSystem { get; set; }

    public Guid? BackupJobId { get; set; }

    public BackupJob? BackupJob { get; set; }

    public string EventType { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    public string Message { get; set; } = string.Empty;

    public string? RawPayloadJson { get; set; }
}