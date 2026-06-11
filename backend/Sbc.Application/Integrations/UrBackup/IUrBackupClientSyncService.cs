namespace Sbc.Application.Integrations.UrBackup;

public interface IUrBackupClientSyncService
{
    Task<UrBackupClientSyncResult> SyncClientsAsync(CancellationToken cancellationToken);
}