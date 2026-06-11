namespace Sbc.Domain.Enums;

public enum BackupJobStatus
{
    Unknown = 1,
    Running = 2,
    Success = 3,
    Failed = 4,
    Cancelled = 5,
    PendingValidation = 6
}