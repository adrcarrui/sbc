using Sbc.Domain.Common;
using Sbc.Domain.Enums;

namespace Sbc.Domain.Entities;

public class ProtectedSystem : AuditableEntity
{
    public Guid? SimulatorId { get; set; }

    public Simulator? Simulator { get; set; }

    public string Hostname { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? OperatingSystem { get; set; }

    public string? FileSystem { get; set; }

    public string? PartitionScheme { get; set; }

    public string? UrBackupClientId { get; set; }

    public string? UrBackupClientName { get; set; }

    public string? UrBackupClientVersion { get; set; }

    public Criticality Criticality { get; set; } = Criticality.Medium;

    public BackupCapability BackupCapability { get; set; } = BackupCapability.PendingValidation;

    public bool FileBackupValidated { get; set; }

    public bool ImageBackupValidated { get; set; }

    public bool LiveBackupValidated { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public ICollection<BackupJob> BackupJobs { get; set; } = new List<BackupJob>();

    public ICollection<BackupEvent> BackupEvents { get; set; } = new List<BackupEvent>();

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    public bool IsOnline { get; set; }

    public DateTime? LastSeenAtUtc { get; set; }

    public DateTime? LastFileBackupAtUtc { get; set; }

    public DateTime? LastImageBackupAtUtc { get; set; }

    public bool LastFileBackupOk { get; set; }

    public bool LastImageBackupOk { get; set; }

    public int? LastFileBackupIssues { get; set; }

    public int? UrBackupStatusCode { get; set; }
}