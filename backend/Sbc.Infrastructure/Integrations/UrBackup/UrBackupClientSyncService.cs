using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Sbc.Application.Integrations.UrBackup;
using Sbc.Domain.Entities;
using Sbc.Domain.Enums;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Infrastructure.Integrations.UrBackup;

public sealed class UrBackupClientSyncService : IUrBackupClientSyncService
{
    private const string ClientRemovedAlertCode = "URBACKUP_CLIENT_REMOVED";

    private const string BackupWithIssuesAlertCode = "URBACKUP_BACKUP_WITH_ISSUES";

    private const string NoSuccessfulBackupAlertCode = "URBACKUP_NO_SUCCESSFUL_BACKUP";

    private readonly IUrBackupClient _urBackupClient;
    private readonly SbcDbContext _dbContext;

    public UrBackupClientSyncService(
        IUrBackupClient urBackupClient,
        SbcDbContext dbContext)
    {
        _urBackupClient = urBackupClient;
        _dbContext = dbContext;
    }

    public async Task<UrBackupClientSyncResult> SyncClientsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rawStatus = await _urBackupClient.GetRawStatusAsync(cancellationToken);

        if (!rawStatus.Success || string.IsNullOrWhiteSpace(rawStatus.RawJson))
        {
            return Failed(
                "Could not retrieve UrBackup status.",
                rawStatus.ErrorMessage);
        }

        JsonNode? root;

        try
        {
            root = JsonNode.Parse(rawStatus.RawJson);
        }
        catch (Exception ex)
        {
            return Failed(
                "Could not parse UrBackup status response.",
                ex.Message);
        }

        var statusArray = root?["status"]?.AsArray();

        if (statusArray is null)
        {
            return Failed(
                "UrBackup status response does not contain a valid 'status' array.",
                null);
        }

        var createdClients = 0;
        var updatedClients = 0;
        var restoredClients = 0;
        var removedClients = 0;
        var skippedClients = 0;

        var syncedClientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var syncedClientNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var syncedClients = new List<UrBackupSyncedClientResult>();

        foreach (var clientNode in statusArray)
        {
            if (clientNode is null)
            {
                skippedClients++;
                continue;
            }

            var clientId = NormalizeOptional(GetString(clientNode, "id"));
            var clientName = NormalizeOptional(GetString(clientNode, "name"));

            if (string.IsNullOrWhiteSpace(clientName))
            {
                skippedClients++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                syncedClientIds.Add(clientId);
            }

            syncedClientNames.Add(clientName);

            var existingSystem = await FindExistingProtectedSystemAsync(
                clientId,
                clientName,
                cancellationToken);

            var wasCreated = false;
            var wasRestored = false;

            if (existingSystem is null)
            {
                existingSystem = new ProtectedSystem
                {
                    Hostname = clientName,
                    Criticality = Criticality.Medium,
                    BackupCapability = BackupCapability.PendingValidation,
                    IsActive = true
                };

                _dbContext.ProtectedSystems.Add(existingSystem);
                wasCreated = true;
                createdClients++;
            }
            else
            {
                wasRestored = existingSystem.IsRemovedFromUrBackup;

                if (wasRestored)
                {
                    restoredClients++;
                }
                else
                {
                    updatedClients++;
                }
            }

            ApplyUrBackupClientStatus(existingSystem, clientNode, clientId, clientName, now);

            UpdateBackupCapabilityFromUrBackup(existingSystem);

            await SyncBackupHealthAlertsAsync(existingSystem, cancellationToken);

            await SyncLatestUrBackupJobsAsync(existingSystem, cancellationToken);

            if (wasCreated)
            {
                _dbContext.BackupEvents.Add(new BackupEvent
                {
                    ProtectedSystem = existingSystem,
                    EventType = "urbackup_client_discovered",
                    Severity = AlertSeverity.Info,
                    Message = $"UrBackup client discovered: {clientName}."
                });
            }
            else if (wasRestored)
            {
                _dbContext.BackupEvents.Add(new BackupEvent
                {
                    ProtectedSystem = existingSystem,
                    EventType = "urbackup_client_restored",
                    Severity = AlertSeverity.Info,
                    Message = $"UrBackup client is present again: {clientName}."
                });

                await ResolveClientRemovedAlertIfOpenAsync(existingSystem.Id, cancellationToken);
            }

            syncedClients.Add(new UrBackupSyncedClientResult(
                UrBackupClientId: clientId,
                Name: clientName,
                Online: existingSystem.IsOnline,
                OperatingSystem: existingSystem.OperatingSystem,
                LastSeenAtUtc: existingSystem.LastSeenAtUtc,
                LastFileBackupAtUtc: existingSystem.LastFileBackupAtUtc,
                LastImageBackupAtUtc: existingSystem.LastImageBackupAtUtc,
                FileBackupOk: existingSystem.LastFileBackupOk,
                ImageBackupOk: existingSystem.LastImageBackupOk,
                IsRemovedFromUrBackup: existingSystem.IsRemovedFromUrBackup,
                LastUrBackupSyncAtUtc: existingSystem.LastUrBackupSyncAtUtc,
                Action: wasCreated
                    ? "created"
                    : wasRestored
                        ? "restored"
                        : "updated"));
        }

        removedClients = await MarkMissingUrBackupClientsAsRemovedAsync(
            syncedClientIds,
            syncedClientNames,
            now,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UrBackupClientSyncResult(
            Success: true,
            Message: "UrBackup clients synchronized successfully.",
            ErrorMessage: null,
            DiscoveredClients: statusArray.Count,
            CreatedClients: createdClients,
            UpdatedClients: updatedClients,
            RestoredClients: restoredClients,
            RemovedClients: removedClients,
            SkippedClients: skippedClients,
            SyncedClients: syncedClients);
    }

    private async Task<ProtectedSystem?> FindExistingProtectedSystemAsync(
        string? clientId,
        string clientName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var byClientId = await _dbContext.ProtectedSystems
                .FirstOrDefaultAsync(
                    x => x.UrBackupClientId == clientId,
                    cancellationToken);

            if (byClientId is not null)
            {
                return byClientId;
            }
        }

        return await _dbContext.ProtectedSystems
            .FirstOrDefaultAsync(
                x => x.UrBackupClientName == clientName ||
                     x.Hostname == clientName,
                cancellationToken);
    }

    private async Task<int> MarkMissingUrBackupClientsAsRemovedAsync(
        HashSet<string> syncedClientIds,
        HashSet<string> syncedClientNames,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var trackedSystems = await _dbContext.ProtectedSystems
            .Where(x =>
                !x.IsRemovedFromUrBackup &&
                (x.UrBackupClientId != null || x.UrBackupClientName != null))
            .ToListAsync(cancellationToken);

        var removedClients = 0;

        foreach (var system in trackedSystems)
        {
            var stillExistsById =
                !string.IsNullOrWhiteSpace(system.UrBackupClientId) &&
                syncedClientIds.Contains(system.UrBackupClientId);

            var stillExistsByUrBackupName =
                !string.IsNullOrWhiteSpace(system.UrBackupClientName) &&
                syncedClientNames.Contains(system.UrBackupClientName);

            var stillExistsByHostname =
                !string.IsNullOrWhiteSpace(system.Hostname) &&
                syncedClientNames.Contains(system.Hostname);

            if (stillExistsById || stillExistsByUrBackupName || stillExistsByHostname)
            {
                continue;
            }

            system.IsRemovedFromUrBackup = true;
            system.RemovedFromUrBackupAtUtc = now;
            system.LastUrBackupSyncAtUtc = now;
            system.IsActive = false;
            system.IsOnline = false;

            removedClients++;

            _dbContext.BackupEvents.Add(new BackupEvent
            {
                ProtectedSystemId = system.Id,
                EventType = "urbackup_client_removed",
                Severity = AlertSeverity.Warning,
                Message = $"UrBackup client no longer appears in UrBackup status: {system.Hostname}."
            });

            await CreateClientRemovedAlertIfMissingAsync(system, cancellationToken);
        }

        return removedClients;
    }

    private async Task SyncBackupHealthAlertsAsync(
        ProtectedSystem system,
        CancellationToken cancellationToken)
    {
        if (system.IsRemovedFromUrBackup)
        {
            return;
        }

        var hasBackupIssues = (system.LastFileBackupIssues ?? 0) > 0;
        var hasAnySuccessfulBackup = system.LastFileBackupOk || system.LastImageBackupOk;

        if (hasBackupIssues)
        {
            await CreateAlertIfMissingAsync(
                system,
                BackupWithIssuesAlertCode,
                "Backup with issues",
                $"System '{system.Hostname}' has {system.LastFileBackupIssues} issue(s) in the last file backup.",
                AlertSeverity.Warning,
                cancellationToken);

            await ResolveAlertIfOpenAsync(
                system.Id,
                NoSuccessfulBackupAlertCode,
                cancellationToken);

            return;
        }

        await ResolveAlertIfOpenAsync(
            system.Id,
            BackupWithIssuesAlertCode,
            cancellationToken);

        if (!hasAnySuccessfulBackup)
        {
            await CreateAlertIfMissingAsync(
                system,
                NoSuccessfulBackupAlertCode,
                "No successful backup",
                $"System '{system.Hostname}' does not have a successful file or image backup recorded.",
                AlertSeverity.Warning,
                cancellationToken);

            return;
        }

        await ResolveAlertIfOpenAsync(
            system.Id,
            NoSuccessfulBackupAlertCode,
            cancellationToken);
    }

    private async Task CreateClientRemovedAlertIfMissingAsync(
        ProtectedSystem system,
        CancellationToken cancellationToken)
    {
        var existingOpenAlert = await _dbContext.Alerts
            .AnyAsync(
                x => x.ProtectedSystemId == system.Id &&
                     x.Code == ClientRemovedAlertCode &&
                     x.Status == AlertStatus.Open,
                cancellationToken);

        if (existingOpenAlert)
        {
            return;
        }

        _dbContext.Alerts.Add(new Alert
        {
            ProtectedSystemId = system.Id,
            Code = ClientRemovedAlertCode,
            Title = "UrBackup client removed",
            Message = $"System '{system.Hostname}' is registered in SBC but no longer appears in UrBackup.",
            Severity = AlertSeverity.Warning,
            Status = AlertStatus.Open
        });
    }

    private async Task CreateAlertIfMissingAsync(
        ProtectedSystem system,
        string code,
        string title,
        string message,
        AlertSeverity severity,
        CancellationToken cancellationToken)
    {
        var existingOpenAlert = await _dbContext.Alerts
            .AnyAsync(
                x => x.ProtectedSystemId == system.Id &&
                     x.Code == code &&
                     x.Status == AlertStatus.Open,
                cancellationToken);

        if (existingOpenAlert)
        {
            return;
        }

        _dbContext.Alerts.Add(new Alert
        {
            ProtectedSystemId = system.Id,
            Code = code,
            Title = title,
            Message = message,
            Severity = severity,
            Status = AlertStatus.Open
        });

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = system.Id,
            EventType = code.ToLowerInvariant(),
            Severity = severity,
            Message = message
        });
    }

    private async Task ResolveClientRemovedAlertIfOpenAsync(
        Guid protectedSystemId,
        CancellationToken cancellationToken)
    {
        var openAlerts = await _dbContext.Alerts
            .Where(
                x => x.ProtectedSystemId == protectedSystemId &&
                     x.Code == ClientRemovedAlertCode &&
                     x.Status == AlertStatus.Open)
            .ToListAsync(cancellationToken);

        foreach (var alert in openAlerts)
        {
            alert.Status = AlertStatus.Resolved;
            alert.ResolvedAtUtc = DateTime.UtcNow;
        }
    }

    private async Task ResolveAlertIfOpenAsync(
        Guid protectedSystemId,
        string code,
        CancellationToken cancellationToken)
    {
        var openAlerts = await _dbContext.Alerts
            .Where(
                x => x.ProtectedSystemId == protectedSystemId &&
                     x.Code == code &&
                     x.Status == AlertStatus.Open)
            .ToListAsync(cancellationToken);

        foreach (var alert in openAlerts)
        {
            alert.Status = AlertStatus.Resolved;
            alert.ResolvedAtUtc = DateTime.UtcNow;
        }
    }


    private async Task SyncLatestUrBackupJobsAsync(
        ProtectedSystem system,
        CancellationToken cancellationToken)
    {
        if (system.IsRemovedFromUrBackup)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(system.UrBackupClientId))
        {
            return;
        }

        if (system.LastFileBackupAtUtc is not null)
        {
            var fileBackupStatus =
                system.LastFileBackupOk && (system.LastFileBackupIssues ?? 0) == 0
                    ? BackupJobStatus.Success
                    : BackupJobStatus.Failed;

            var fileBackupErrorMessage = fileBackupStatus == BackupJobStatus.Success
                ? null
                : (system.LastFileBackupIssues ?? 0) > 0
                    ? $"UrBackup reported {system.LastFileBackupIssues} issue(s) in the last file backup."
                    : "UrBackup reported the last file backup as not successful.";

            await UpsertUrBackupJobAsync(
                system,
                "file",
                BackupType.UrBackupFile,
                fileBackupStatus,
                system.LastFileBackupAtUtc.Value,
                fileBackupErrorMessage,
                cancellationToken);
        }

        if (system.LastImageBackupAtUtc is not null)
        {
            var imageBackupStatus = system.LastImageBackupOk
                ? BackupJobStatus.Success
                : BackupJobStatus.Failed;

            var imageBackupErrorMessage = imageBackupStatus == BackupJobStatus.Success
                ? null
                : "UrBackup reported the last image backup as not successful.";

            await UpsertUrBackupJobAsync(
                system,
                "image",
                BackupType.UrBackupImage,
                imageBackupStatus,
                system.LastImageBackupAtUtc.Value,
                imageBackupErrorMessage,
                cancellationToken);
        }
    }

    private async Task UpsertUrBackupJobAsync(
        ProtectedSystem system,
        string backupKind,
        BackupType backupType,
        BackupJobStatus status,
        DateTime finishedAtUtc,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var timestamp = finishedAtUtc
            .ToUniversalTime()
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        var urBackupJobId = $"urbackup:{system.UrBackupClientId}:{backupKind}:{timestamp}";

        var existingJob = await _dbContext.BackupJobs
            .FirstOrDefaultAsync(
                x => x.UrBackupJobId == urBackupJobId,
                cancellationToken);

        if (existingJob is not null)
        {
            existingJob.Status = status;
            existingJob.FinishedAtUtc = finishedAtUtc;
            existingJob.ErrorMessage = errorMessage;

            return;
        }

        var backupJob = new BackupJob
        {
            ProtectedSystemId = system.Id,
            Source = BackupSource.UrBackup,
            BackupType = backupType,
            Status = status,
            StartedAtUtc = null,
            FinishedAtUtc = finishedAtUtc,
            DurationSeconds = null,
            SizeBytes = null,
            UrBackupJobId = urBackupJobId,
            BackupPath = null,
            ErrorMessage = errorMessage
        };

        _dbContext.BackupJobs.Add(backupJob);

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = system.Id,
            BackupJob = backupJob,
            EventType = status == BackupJobStatus.Success
                ? $"urbackup_{backupKind}_backup_success"
                : $"urbackup_{backupKind}_backup_failed",
            Severity = status == BackupJobStatus.Success
                ? AlertSeverity.Info
                : AlertSeverity.Warning,
            Message = status == BackupJobStatus.Success
                ? $"UrBackup {backupKind} backup registered successfully for system '{system.Hostname}'."
                : $"UrBackup {backupKind} backup registered with failure for system '{system.Hostname}'. {errorMessage}"
        });
    }

    private static void UpdateBackupCapabilityFromUrBackup(ProtectedSystem system)
    {
        if (system.IsRemovedFromUrBackup)
        {
            return;
        }

        var hasValidFileBackup =
            system.LastFileBackupAtUtc is not null &&
            system.LastFileBackupOk &&
            (system.LastFileBackupIssues ?? 0) == 0;

        var hasValidImageBackup =
            system.LastImageBackupAtUtc is not null &&
            system.LastImageBackupOk;

        system.FileBackupValidated = hasValidFileBackup;
        system.ImageBackupValidated = hasValidImageBackup;

        if (hasValidImageBackup)
        {
            system.BackupCapability = BackupCapability.ImageBackupSupported;
            return;
        }

        if (hasValidFileBackup)
        {
            system.BackupCapability = BackupCapability.FileBackupOnly;
            return;
        }

        if (system.BackupCapability is not BackupCapability.ManualBackupRequired and
            not BackupCapability.NotSupported)
        {
            system.BackupCapability = BackupCapability.PendingValidation;
        }
    }
    private static void ApplyUrBackupClientStatus(
        ProtectedSystem protectedSystem,
        JsonNode clientNode,
        string? clientId,
        string clientName,
        DateTime syncedAtUtc)
    {
        var ipAddress = NormalizeOptional(GetString(clientNode, "ip"));

        if (ipAddress == "-")
        {
            ipAddress = null;
        }

        var osVersion = NormalizeOptional(GetString(clientNode, "os_version_string"));
        var osSimple = NormalizeOptional(GetString(clientNode, "os_simple"));

        protectedSystem.Hostname = clientName;
        protectedSystem.UrBackupClientId = clientId;
        protectedSystem.UrBackupClientName = clientName;
        protectedSystem.UrBackupClientVersion = NormalizeOptional(GetString(clientNode, "client_version_string"));
        protectedSystem.IpAddress = ipAddress;
        protectedSystem.OperatingSystem = osVersion ?? osSimple;

        protectedSystem.IsOnline = GetBool(clientNode, "online");
        protectedSystem.LastSeenAtUtc = FromUnixSeconds(GetLong(clientNode, "lastseen"));
        protectedSystem.LastFileBackupAtUtc = FromUnixSeconds(GetLong(clientNode, "lastbackup"));
        protectedSystem.LastImageBackupAtUtc = FromUnixSeconds(GetLong(clientNode, "lastbackup_image"));
        protectedSystem.LastFileBackupOk = GetBool(clientNode, "file_ok");
        protectedSystem.LastImageBackupOk = GetBool(clientNode, "image_ok");
        protectedSystem.LastFileBackupIssues = GetInt(clientNode, "last_filebackup_issues");
        protectedSystem.UrBackupStatusCode = GetInt(clientNode, "status");
        protectedSystem.LastUrBackupSyncAtUtc = syncedAtUtc;
        protectedSystem.IsRemovedFromUrBackup = false;
        protectedSystem.RemovedFromUrBackupAtUtc = null;
        protectedSystem.IsActive = true;
    }

    private static string? GetString(JsonNode node, string propertyName)
    {
        var value = node[propertyName];

        return value is null
            ? null
            : value.ToString();
    }

    private static bool GetBool(JsonNode node, string propertyName)
    {
        var value = node[propertyName];

        if (value is null)
        {
            return false;
        }

        return value.GetValue<bool>();
    }

    private static long? GetLong(JsonNode node, string propertyName)
    {
        var value = node[propertyName];

        if (value is null)
        {
            return null;
        }

        if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static int? GetInt(JsonNode node, string propertyName)
    {
        var value = GetLong(node, propertyName);

        if (value is null)
        {
            return null;
        }

        if (value > int.MaxValue || value < int.MinValue)
        {
            return null;
        }

        return (int)value.Value;
    }

    private static DateTime? FromUnixSeconds(long? unixSeconds)
    {
        if (unixSeconds is null || unixSeconds <= 0)
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static UrBackupClientSyncResult Failed(string message, string? errorMessage)
    {
        return new UrBackupClientSyncResult(
            Success: false,
            Message: message,
            ErrorMessage: errorMessage,
            DiscoveredClients: 0,
            CreatedClients: 0,
            UpdatedClients: 0,
            RestoredClients: 0,
            RemovedClients: 0,
            SkippedClients: 0,
            SyncedClients: Array.Empty<UrBackupSyncedClientResult>());
    }
}