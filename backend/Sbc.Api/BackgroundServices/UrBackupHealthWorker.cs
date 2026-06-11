using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sbc.Application.Integrations.UrBackup;
using Sbc.Domain.Entities;
using Sbc.Domain.Enums;
using Sbc.Infrastructure.Integrations.UrBackup;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.BackgroundServices;

public sealed class UrBackupHealthWorker : BackgroundService
{
    private const string AlertCode = "URBACKUP_SERVER_UNREACHABLE";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UrBackupHealthWorker> _logger;
    private readonly UrBackupOptions _options;

    public UrBackupHealthWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<UrBackupHealthWorker> logger,
        IOptions<UrBackupOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _options.HealthCheckIntervalSeconds <= 0
            ? 60
            : _options.HealthCheckIntervalSeconds;

        _logger.LogInformation(
            "UrBackup health worker started. Interval: {IntervalSeconds} seconds.",
            intervalSeconds);

        await CheckUrBackupAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckUrBackupAsync(stoppingToken);
        }
    }

    private async Task CheckUrBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var urBackupClient = scope.ServiceProvider.GetRequiredService<IUrBackupClient>();
            var dbContext = scope.ServiceProvider.GetRequiredService<SbcDbContext>();

            var result = await urBackupClient.CheckHealthAsync(cancellationToken);

            if (result.IsReachable)
            {
                await ResolveUnreachableAlertIfOpenAsync(dbContext, cancellationToken);

                _logger.LogDebug(
                    "UrBackup server reachable at {BaseUrl}. StatusCode: {StatusCode}",
                    result.BaseUrl,
                    result.StatusCode);

                return;
            }

            await CreateUnreachableAlertIfMissingAsync(dbContext, result, cancellationToken);

            _logger.LogWarning(
                "UrBackup server unreachable at {BaseUrl}. Error: {ErrorMessage}",
                result.BaseUrl,
                result.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown. No drama, for once.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while checking UrBackup health.");
        }
    }

    private static async Task CreateUnreachableAlertIfMissingAsync(
        SbcDbContext dbContext,
        UrBackupHealthResult result,
        CancellationToken cancellationToken)
    {
        var existingOpenAlert = await dbContext.Alerts
            .FirstOrDefaultAsync(
                x => x.ProtectedSystemId == null &&
                     x.Code == AlertCode &&
                     x.Status == AlertStatus.Open,
                cancellationToken);

        if (existingOpenAlert is not null)
        {
            return;
        }

        var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? $"UrBackup server is unreachable at '{result.BaseUrl}'."
            : $"UrBackup server is unreachable at '{result.BaseUrl}'. Error: {result.ErrorMessage}";

        dbContext.Alerts.Add(new Alert
        {
            ProtectedSystemId = null,
            Code = AlertCode,
            Title = "UrBackup server unreachable",
            Message = message,
            Severity = AlertSeverity.Critical,
            Status = AlertStatus.Open
        });

        dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = null,
            EventType = "urbackup_server_unreachable",
            Severity = AlertSeverity.Critical,
            Message = message,
            RawPayloadJson = null
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task ResolveUnreachableAlertIfOpenAsync(
        SbcDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingOpenAlert = await dbContext.Alerts
            .FirstOrDefaultAsync(
                x => x.ProtectedSystemId == null &&
                     x.Code == AlertCode &&
                     x.Status == AlertStatus.Open,
                cancellationToken);

        if (existingOpenAlert is null)
        {
            return;
        }

        existingOpenAlert.Status = AlertStatus.Resolved;
        existingOpenAlert.ResolvedAtUtc = DateTime.UtcNow;

        dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = null,
            EventType = "urbackup_server_reachable",
            Severity = AlertSeverity.Info,
            Message = "UrBackup server is reachable again."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}