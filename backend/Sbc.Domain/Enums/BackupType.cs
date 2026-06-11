namespace Sbc.Domain.Enums;

public enum BackupType
{
    FileFull = 1,
    FileIncremental = 2,
    ImageFull = 3,
    ImageIncremental = 4,
    ManualDiskClone = 5,
    ManualOther = 6,
    UrBackupFile = 7,
    UrBackupImage = 8
}