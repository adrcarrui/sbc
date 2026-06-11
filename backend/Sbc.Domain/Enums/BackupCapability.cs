namespace Sbc.Domain.Enums;

public enum BackupCapability
{
    PendingValidation = 1,
    FileBackupOnly = 2,
    ImageBackupSupported = 3,
    ImageBackupNotSupported = 4,
    ManualBackupRequired = 5,
    NotSupported = 6
}