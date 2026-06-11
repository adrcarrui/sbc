using Sbc.Domain.Common;
using Sbc.Domain.Enums;

namespace Sbc.Domain.Entities;

public class BackupJob : AuditableEntity
{
    public Guid ProtectedSystemId { get; set; }

    public ProtectedSystem ProtectedSystem { get; set; } = null!;

    public BackupSource Source { get; set; }

    public BackupType BackupType { get; set; }

    public BackupJobStatus Status { get; set; } = BackupJobStatus.Unknown;

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public int? DurationSeconds { get; set; }

    public long? SizeBytes { get; set; }

    public string? UrBackupJobId { get; set; }

    public string? BackupPath { get; set; }

    public string? ErrorMessage { get; set; }

    public ICollection<BackupEvent> Events { get; set; } = new List<BackupEvent>();
}