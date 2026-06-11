using Sbc.Domain.Common;
using Sbc.Domain.Enums;

namespace Sbc.Domain.Entities;

public class Alert : AuditableEntity
{
    public Guid? ProtectedSystemId { get; set; }

    public ProtectedSystem? ProtectedSystem { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    public AlertStatus Status { get; set; } = AlertStatus.Open;

    public DateTime? ResolvedAtUtc { get; set; }
}