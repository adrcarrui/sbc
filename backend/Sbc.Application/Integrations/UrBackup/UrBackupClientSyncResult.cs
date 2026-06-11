namespace Sbc.Application.Integrations.UrBackup;

public sealed record UrBackupClientSyncResult(
    bool Success,
    string? Message,
    string? ErrorMessage,
    int DiscoveredClients,
    int CreatedClients,
    int UpdatedClients,
    int RestoredClients,
    int RemovedClients,
    int SkippedClients,
    IReadOnlyList<UrBackupSyncedClientResult> SyncedClients);

public sealed record UrBackupSyncedClientResult(
    string? UrBackupClientId,
    string Name,
    bool Online,
    string? OperatingSystem,
    DateTime? LastSeenAtUtc,
    DateTime? LastFileBackupAtUtc,
    DateTime? LastImageBackupAtUtc,
    bool FileBackupOk,
    bool ImageBackupOk,
    bool IsRemovedFromUrBackup,
    DateTime? LastUrBackupSyncAtUtc,
    string Action);