using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Domain.Enums;
using Sbc.Domain.Entities;
using Sbc.Infrastructure.Persistence;


namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public DashboardController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var systems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalSystems = systems.Count;

        var urBackupIntegratedSystems = systems.Count(x =>
            !string.IsNullOrWhiteSpace(x.UrBackupClientId) ||
            !string.IsNullOrWhiteSpace(x.UrBackupClientName));

        var manualOrPendingSystems = systems.Count(x =>
            string.IsNullOrWhiteSpace(x.UrBackupClientId) &&
            string.IsNullOrWhiteSpace(x.UrBackupClientName));

        var onlineSystems = systems.Count(x =>
            x.IsActive &&
            !x.IsRemovedFromUrBackup &&
            x.IsOnline);

        var offlineSystems = systems.Count(x =>
            x.IsActive &&
            !x.IsRemovedFromUrBackup &&
            !x.IsOnline);

        var removedFromUrBackupSystems = systems.Count(x =>
            x.IsRemovedFromUrBackup);

        var backupOkSystems = systems.Count(x =>
            !x.IsRemovedFromUrBackup &&
            (x.LastFileBackupOk || x.LastImageBackupOk) &&
            (x.LastFileBackupIssues ?? 0) == 0);

        var backupWithIssuesSystems = systems.Count(x =>
            !x.IsRemovedFromUrBackup &&
            (x.LastFileBackupIssues ?? 0) > 0);

        var noSuccessfulBackupSystems = systems.Count(x =>
            !x.IsRemovedFromUrBackup &&
            !x.LastFileBackupOk &&
            !x.LastImageBackupOk &&
            (x.LastFileBackupIssues ?? 0) == 0);

        var openAlerts = await _dbContext.Alerts
            .AsNoTracking()
            .CountAsync(x => x.Status == Domain.Enums.AlertStatus.Open, cancellationToken);

        var lastUrBackupSyncAtUtc = systems
            .Where(x => x.LastUrBackupSyncAtUtc != null)
            .Select(x => x.LastUrBackupSyncAtUtc)
            .DefaultIfEmpty()
            .Max();

        return Ok(new
        {
            TotalSystems = totalSystems,
            UrBackupIntegratedSystems = urBackupIntegratedSystems,
            ManualOrPendingSystems = manualOrPendingSystems,
            OnlineSystems = onlineSystems,
            OfflineSystems = offlineSystems,
            RemovedFromUrBackupSystems = removedFromUrBackupSystems,
            BackupOkSystems = backupOkSystems,
            BackupWithIssuesSystems = backupWithIssuesSystems,
            NoSuccessfulBackupSystems = noSuccessfulBackupSystems,
            OpenAlerts = openAlerts,
            LastUrBackupSyncAtUtc = lastUrBackupSyncAtUtc
        });
    }

    [HttpGet("urbackup-systems")]
    public async Task<IActionResult> GetUrBackupSystems(CancellationToken cancellationToken)
    {
        var systems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .Include(x => x.Simulator)
            .OrderBy(x => x.Hostname)
            .Select(x => new
            {
                x.Id,
                x.Hostname,
                x.IpAddress,
                x.OperatingSystem,
                x.UrBackupClientId,
                x.UrBackupClientName,
                x.UrBackupClientVersion,
                x.IsActive,
                x.IsOnline,
                x.IsRemovedFromUrBackup,
                x.LastUrBackupSyncAtUtc,
                x.LastSeenAtUtc,
                x.LastFileBackupAtUtc,
                x.LastImageBackupAtUtc,
                x.LastFileBackupOk,
                x.LastImageBackupOk,
                x.LastFileBackupIssues,
                x.UrBackupStatusCode,

                OperationalStatus = x.IsRemovedFromUrBackup
                    ? "RemovedFromUrBackup"
                    : !x.IsActive
                        ? "Inactive"
                        : x.IsOnline
                            ? "Online"
                            : "Offline",

                BackupStatus = x.IsRemovedFromUrBackup
                    ? "RemovedFromUrBackup"
                    : (x.LastFileBackupIssues ?? 0) > 0
                        ? "WithIssues"
                        : x.LastFileBackupOk || x.LastImageBackupOk
                            ? "Ok"
                            : "NoSuccessfulBackup",

                Simulator = x.Simulator == null
                    ? null
                    : new
                    {
                        x.Simulator.Id,
                        x.Simulator.Code,
                        x.Simulator.Name
                    }
            })
            .ToListAsync(cancellationToken);

        return Ok(systems);
    }

    [HttpGet("attention-systems")]
    public async Task<IActionResult> GetAttentionSystems(CancellationToken cancellationToken)
    {
        var systems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .Include(x => x.Simulator)
            .ToListAsync(cancellationToken);

        var attentionSystems = systems
            .Where(NeedsAttention)
            .OrderByDescending(GetAttentionPriority)
            .ThenBy(x => x.Hostname)
            .Select(x => new
            {
                x.Id,
                x.Hostname,
                x.IpAddress,
                x.OperatingSystem,
                x.UrBackupClientId,
                x.UrBackupClientName,
                x.IsActive,
                x.IsOnline,
                x.IsRemovedFromUrBackup,
                x.LastUrBackupSyncAtUtc,
                x.LastSeenAtUtc,
                x.LastFileBackupAtUtc,
                x.LastImageBackupAtUtc,
                x.LastFileBackupOk,
                x.LastImageBackupOk,
                x.LastFileBackupIssues,

                Severity = GetAttentionSeverity(x),
                Reason = GetAttentionReason(x),
                Description = GetAttentionDescription(x),

                Simulator = x.Simulator == null
                    ? null
                    : new
                    {
                        x.Simulator.Id,
                        x.Simulator.Code,
                        x.Simulator.Name
                    }
            })
            .ToList();

        return Ok(attentionSystems);

        static bool NeedsAttention(ProtectedSystem system)
        {
            var isIntegratedWithUrBackup =
                !string.IsNullOrWhiteSpace(system.UrBackupClientId) ||
                !string.IsNullOrWhiteSpace(system.UrBackupClientName);

            return system.IsRemovedFromUrBackup ||
                   !isIntegratedWithUrBackup ||
                   (system.LastFileBackupIssues ?? 0) > 0 ||
                   (!system.LastFileBackupOk && !system.LastImageBackupOk);
        }

        static int GetAttentionPriority(ProtectedSystem system)
        {
            var isIntegratedWithUrBackup =
                !string.IsNullOrWhiteSpace(system.UrBackupClientId) ||
                !string.IsNullOrWhiteSpace(system.UrBackupClientName);

            if (system.IsRemovedFromUrBackup)
            {
                return 100;
            }

            if (!isIntegratedWithUrBackup)
            {
                return 80;
            }

            if ((system.LastFileBackupIssues ?? 0) > 0)
            {
                return 70;
            }

            if (!system.LastFileBackupOk && !system.LastImageBackupOk)
            {
                return 60;
            }

            return 0;
        }

        static string GetAttentionSeverity(ProtectedSystem system)
        {
            var isIntegratedWithUrBackup =
                !string.IsNullOrWhiteSpace(system.UrBackupClientId) ||
                !string.IsNullOrWhiteSpace(system.UrBackupClientName);

            if (system.IsRemovedFromUrBackup)
            {
                return "Critical";
            }

            if (!isIntegratedWithUrBackup)
            {
                return "Warning";
            }

            if ((system.LastFileBackupIssues ?? 0) > 0)
            {
                return "Warning";
            }

            if (!system.LastFileBackupOk && !system.LastImageBackupOk)
            {
                return "Warning";
            }

            return "Info";
        }

        static string GetAttentionReason(ProtectedSystem system)
        {
            var isIntegratedWithUrBackup =
                !string.IsNullOrWhiteSpace(system.UrBackupClientId) ||
                !string.IsNullOrWhiteSpace(system.UrBackupClientName);

            if (system.IsRemovedFromUrBackup)
            {
                return "RemovedFromUrBackup";
            }

            if (!isIntegratedWithUrBackup)
            {
                return "PendingUrBackupIntegration";
            }

            if ((system.LastFileBackupIssues ?? 0) > 0)
            {
                return "BackupWithIssues";
            }

            if (!system.LastFileBackupOk && !system.LastImageBackupOk)
            {
                return "NoSuccessfulBackup";
            }

            return "AttentionRequired";
        }

        static string GetAttentionDescription(ProtectedSystem system)
        {
            var isIntegratedWithUrBackup =
                !string.IsNullOrWhiteSpace(system.UrBackupClientId) ||
                !string.IsNullOrWhiteSpace(system.UrBackupClientName);

            if (system.IsRemovedFromUrBackup)
            {
                return "The system exists in SBC but no longer appears in UrBackup.";
            }

            if (!isIntegratedWithUrBackup)
            {
                return "The system exists in SBC but is not linked to a UrBackup client.";
            }

            if ((system.LastFileBackupIssues ?? 0) > 0)
            {
                return $"The last file backup reported {system.LastFileBackupIssues} issue(s).";
            }

            if (!system.LastFileBackupOk && !system.LastImageBackupOk)
            {
                return "The system does not have a successful file or image backup recorded.";
            }

            return "The system requires attention.";
        }
    }

    [HttpGet("recent-backup-events")]
    [HttpGet("recent-events")]
    public async Task<IActionResult> GetRecentBackupEvents(
    [FromQuery] int take,
    CancellationToken cancellationToken)
    {
        take = take <= 0
            ? 25
            : Math.Clamp(take, 1, 100);

        var events = await _dbContext.BackupEvents
            .AsNoTracking()
            .Include(x => x.ProtectedSystem)
                .ThenInclude(x => x!.Simulator)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.EventType,
                x.Severity,
                x.Message,
                x.CreatedAtUtc,

                ProtectedSystem = x.ProtectedSystem == null
                    ? null
                    : new
                    {
                        x.ProtectedSystem.Id,
                        x.ProtectedSystem.Hostname,
                        x.ProtectedSystem.IpAddress,
                        x.ProtectedSystem.OperatingSystem,
                        x.ProtectedSystem.IsOnline,
                        x.ProtectedSystem.IsRemovedFromUrBackup,

                        Simulator = x.ProtectedSystem.Simulator == null
                            ? null
                            : new
                            {
                                x.ProtectedSystem.Simulator.Id,
                                x.ProtectedSystem.Simulator.Code,
                                x.ProtectedSystem.Simulator.Name
                            }
                    }
            })
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    [HttpGet("latest-backups")]
    public async Task<IActionResult> GetLatestBackups(CancellationToken cancellationToken)
    {
        var systems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .Include(x => x.Simulator)
            .OrderBy(x => x.Hostname)
            .ToListAsync(cancellationToken);

        var systemIds = systems
            .Select(x => x.Id)
            .ToList();

        var jobs = await _dbContext.BackupJobs
            .AsNoTracking()
            .Where(x => systemIds.Contains(x.ProtectedSystemId))
            .ToListAsync(cancellationToken);

        var result = systems
            .Select(system =>
            {
                var systemJobs = jobs
                    .Where(x => x.ProtectedSystemId == system.Id)
                    .OrderByDescending(GetBackupJobSortDate)
                    .ToList();

                var latestFileBackup = systemJobs
                    .Where(IsFileBackup)
                    .OrderByDescending(GetBackupJobSortDate)
                    .FirstOrDefault();

                var latestImageBackup = systemJobs
                    .Where(IsImageOrDiskBackup)
                    .OrderByDescending(GetBackupJobSortDate)
                    .FirstOrDefault();

                var latestAnyBackup = systemJobs
                    .FirstOrDefault();

                var latestStatus = GetLatestBackupStatus(
                    system,
                    latestAnyBackup,
                    latestFileBackup,
                    latestImageBackup);

                return new
                {
                    system.Id,
                    system.Hostname,
                    system.IpAddress,
                    system.OperatingSystem,
                    system.IsActive,
                    system.IsOnline,
                    system.IsRemovedFromUrBackup,

                    IsIntegratedWithUrBackup =
                        !string.IsNullOrWhiteSpace(system.UrBackupClientId) ||
                        !string.IsNullOrWhiteSpace(system.UrBackupClientName),

                    system.BackupCapability,
                    system.FileBackupValidated,
                    system.ImageBackupValidated,
                    system.LastUrBackupSyncAtUtc,

                    LatestStatus = latestStatus,
                    RequiresAttention = latestStatus is
                        "NoBackupJob" or
                        "Failed" or
                        "PendingValidation" or
                        "RemovedFromUrBackup",

                    LatestFileBackup = ToBackupJobDto(latestFileBackup),
                    LatestImageBackup = ToBackupJobDto(latestImageBackup),
                    LatestAnyBackup = ToBackupJobDto(latestAnyBackup),

                    Simulator = system.Simulator == null
                        ? null
                        : new
                        {
                            system.Simulator.Id,
                            system.Simulator.Code,
                            system.Simulator.Name
                        }
                };
            })
            .ToList();

        return Ok(result);

        static bool IsFileBackup(Sbc.Domain.Entities.BackupJob job)
        {
            return job.BackupType is
                Sbc.Domain.Enums.BackupType.FileFull or
                Sbc.Domain.Enums.BackupType.FileIncremental or
                Sbc.Domain.Enums.BackupType.UrBackupFile;
        }

        static bool IsImageOrDiskBackup(Sbc.Domain.Entities.BackupJob job)
        {
            return job.BackupType is
                Sbc.Domain.Enums.BackupType.ImageFull or
                Sbc.Domain.Enums.BackupType.ImageIncremental or
                Sbc.Domain.Enums.BackupType.ManualDiskClone or
                Sbc.Domain.Enums.BackupType.UrBackupImage;
        }

        static DateTime GetBackupJobSortDate(Sbc.Domain.Entities.BackupJob job)
        {
            return job.FinishedAtUtc ??
                   job.StartedAtUtc ??
                   DateTime.MinValue;
        }

        static string GetLatestBackupStatus(
            Sbc.Domain.Entities.ProtectedSystem system,
            Sbc.Domain.Entities.BackupJob? latestAnyBackup,
            Sbc.Domain.Entities.BackupJob? latestFileBackup,
            Sbc.Domain.Entities.BackupJob? latestImageBackup)
        {
            if (system.IsRemovedFromUrBackup)
            {
                return "RemovedFromUrBackup";
            }

            if (latestAnyBackup is null)
            {
                return "NoBackupJob";
            }

            if (latestFileBackup?.Status == Sbc.Domain.Enums.BackupJobStatus.Failed ||
                latestImageBackup?.Status == Sbc.Domain.Enums.BackupJobStatus.Failed)
            {
                return "Failed";
            }

            if (latestFileBackup?.Status == Sbc.Domain.Enums.BackupJobStatus.PendingValidation ||
                latestImageBackup?.Status == Sbc.Domain.Enums.BackupJobStatus.PendingValidation)
            {
                return "PendingValidation";
            }

            if (latestFileBackup?.Status == Sbc.Domain.Enums.BackupJobStatus.Success ||
                latestImageBackup?.Status == Sbc.Domain.Enums.BackupJobStatus.Success)
            {
                return "Success";
            }

            return latestAnyBackup.Status.ToString();
        }

        static object? ToBackupJobDto(Sbc.Domain.Entities.BackupJob? job)
        {
            if (job is null)
            {
                return null;
            }

            return new
            {
                job.Id,
                job.Source,
                job.BackupType,
                job.Status,
                job.StartedAtUtc,
                job.FinishedAtUtc,
                job.DurationSeconds,
                job.SizeBytes,
                job.BackupPath,
                job.UrBackupJobId,
                job.ErrorMessage
            };
        }
    }
    
}