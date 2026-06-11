using Sbc.Domain.Common;
using Sbc.Domain.Enums;

namespace Sbc.Domain.Entities;

public class ManualBackupRequest : AuditableEntity
{
    public Guid ProtectedSystemId { get; set; }

    public ProtectedSystem ProtectedSystem { get; set; } = null!;

    public string RequestedBy { get; set; } = string.Empty;

    public string? AssignedTo { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? RelatedChangeReference { get; set; }

    public ManualBackupRequestStatus Status { get; set; } = ManualBackupRequestStatus.Pending;

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public string? ValidatedBy { get; set; }

    public DateTime? ValidatedAtUtc { get; set; }

    public string? ValidationNotes { get; set; }
}