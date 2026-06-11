namespace Sbc.Application.Integrations.UrBackup;

public interface IUrBackupClient
{
    Task<UrBackupHealthResult> CheckHealthAsync(CancellationToken cancellationToken);

    Task<UrBackupRawStatusResult> GetRawStatusAsync(CancellationToken cancellationToken);
}