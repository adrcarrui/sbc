namespace Sbc.Domain.Enums;

public enum ManualBackupRequestStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Validated = 4,
    Rejected = 5,
    Cancelled = 6
}