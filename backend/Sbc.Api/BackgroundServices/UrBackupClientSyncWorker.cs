using Microsoft.Extensions.Options;
using Sbc.Application.Integrations.UrBackup;
using Sbc.Infrastructure.Integrations.UrBackup;

namespace Sbc.Api.BackgroundServices;

public sealed class UrBackupClientSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<UrBackupOptions> _options;
    private readonly ILogger<UrBackupClientSyncWorker> _logger;

    public UrBackupClientSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<UrBackupOptions> options,
        ILogger<UrBackupClientSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        if (!options.EnableClientSyncWorker)
        {
            _logger.LogInformation("UrBackup client sync worker is disabled.");
            return;
        }

        var intervalSeconds = options.ClientSyncIntervalSeconds <= 0
            ? 300
            : options.ClientSyncIntervalSeconds;

        var interval = TimeSpan.FromSeconds(intervalSeconds);

        _logger.LogInformation(
            "UrBackup client sync worker started. Interval: {IntervalSeconds} seconds.",
            intervalSeconds);

        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("UrBackup client sync worker stopped.");
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var syncService = scope.ServiceProvider
                .GetRequiredService<IUrBackupClientSyncService>();

            var result = await syncService.SyncClientsAsync(cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "UrBackup client sync failed. Message: {Message}. Error: {ErrorMessage}",
                    result.Message,
                    result.ErrorMessage);

                return;
            }

            _logger.LogInformation(
                "UrBackup client sync completed. Discovered: {DiscoveredClients}. Created: {CreatedClients}. Updated: {UpdatedClients}. Restored: {RestoredClients}. Removed: {RemovedClients}. Skipped: {SkippedClients}.",
                result.DiscoveredClients,
                result.CreatedClients,
                result.UpdatedClients,
                result.RestoredClients,
                result.RemovedClients,
                result.SkippedClients);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while synchronizing UrBackup clients.");
        }
    }
}