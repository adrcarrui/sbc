using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Domain.Entities;
using Sbc.Domain.Enums;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public AlertsController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var alerts = await _dbContext.Alerts
            .AsNoTracking()
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Severity)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Title,
                x.Message,
                Severity = x.Severity.ToString(),
                Status = x.Status.ToString(),
                x.CreatedAtUtc,
                x.ResolvedAtUtc,
                ProtectedSystem = x.ProtectedSystem == null
                    ? null
                    : new
                    {
                        x.ProtectedSystem.Id,
                        x.ProtectedSystem.Hostname,
                        x.ProtectedSystem.IpAddress,
                        x.ProtectedSystem.OperatingSystem,
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

        return Ok(alerts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _dbContext.Alerts
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Title,
                x.Message,
                Severity = x.Severity.ToString(),
                Status = x.Status.ToString(),
                x.CreatedAtUtc,
                x.ResolvedAtUtc,
                ProtectedSystem = x.ProtectedSystem == null
                    ? null
                    : new
                    {
                        x.ProtectedSystem.Id,
                        x.ProtectedSystem.Hostname,
                        x.ProtectedSystem.IpAddress,
                        x.ProtectedSystem.OperatingSystem,
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
            .FirstOrDefaultAsync(cancellationToken);

        if (alert is null)
        {
            return NotFound();
        }

        return Ok(alert);
    }

    [HttpPut("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _dbContext.Alerts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (alert is null)
        {
            return NotFound();
        }

        if (alert.Status == AlertStatus.Resolved)
        {
            return NoContent();
        }

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedAtUtc = DateTime.UtcNow;

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = alert.ProtectedSystemId,
            EventType = "alert_resolved",
            Severity = AlertSeverity.Info,
            Message = $"Alert resolved: {alert.Code} - {alert.Title}"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("recalculate")]
    public async Task<IActionResult> Recalculate(CancellationToken cancellationToken)
    {
        var createdAlerts = 0;

        var systems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Hostname)
            .ToListAsync(cancellationToken);

        foreach (var system in systems)
        {
            if (system.BackupCapability == BackupCapability.PendingValidation)
            {
                createdAlerts += await CreateAlertIfMissing(
                    protectedSystemId: system.Id,
                    code: "BACKUP_VALIDATION_PENDING",
                    title: "Backup validation pending",
                    message: $"System '{system.Hostname}' has not completed backup capability validation.",
                    severity: AlertSeverity.Warning,
                    cancellationToken);
            }

            if (system.BackupCapability == BackupCapability.ManualBackupRequired)
            {
                createdAlerts += await CreateAlertIfMissing(
                    protectedSystemId: system.Id,
                    code: "MANUAL_BACKUP_REQUIRED",
                    title: "Manual backup required",
                    message: $"System '{system.Hostname}' requires a manual backup workflow.",
                    severity: AlertSeverity.Warning,
                    cancellationToken);
            }

            var hasSuccessfulBackup = await _dbContext.BackupJobs
                .AsNoTracking()
                .AnyAsync(
                    x => x.ProtectedSystemId == system.Id &&
                         x.Status == BackupJobStatus.Success,
                    cancellationToken);

            if (!hasSuccessfulBackup)
            {
                createdAlerts += await CreateAlertIfMissing(
                    protectedSystemId: system.Id,
                    code: "NO_SUCCESSFUL_BACKUP_REGISTERED",
                    title: "No successful backup registered",
                    message: $"System '{system.Hostname}' does not have any successful backup registered.",
                    severity: AlertSeverity.Warning,
                    cancellationToken);
            }

            var latestBackup = await _dbContext.BackupJobs
                .AsNoTracking()
                .Where(x => x.ProtectedSystemId == system.Id)
                .OrderByDescending(x => x.FinishedAtUtc ?? x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestBackup is not null && latestBackup.Status == BackupJobStatus.Failed)
            {
                createdAlerts += await CreateAlertIfMissing(
                    protectedSystemId: system.Id,
                    code: "LAST_BACKUP_FAILED",
                    title: "Last backup failed",
                    message: $"The latest backup registered for system '{system.Hostname}' failed.",
                    severity: AlertSeverity.Error,
                    cancellationToken);
            }
        }

        var pendingManualRequests = await _dbContext.ManualBackupRequests
            .AsNoTracking()
            .Include(x => x.ProtectedSystem)
            .Where(x =>
                x.Status == ManualBackupRequestStatus.Pending ||
                x.Status == ManualBackupRequestStatus.InProgress)
            .ToListAsync(cancellationToken);

        foreach (var request in pendingManualRequests)
        {
            createdAlerts += await CreateAlertIfMissing(
                protectedSystemId: request.ProtectedSystemId,
                code: "MANUAL_BACKUP_REQUEST_PENDING",
                title: "Manual backup request pending",
                message: $"Manual backup request is pending for system '{request.ProtectedSystem.Hostname}'.",
                severity: AlertSeverity.Warning,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            createdAlerts
        });
    }

    private async Task<int> CreateAlertIfMissing(
        Guid? protectedSystemId,
        string code,
        string title,
        string message,
        AlertSeverity severity,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Alerts
            .AnyAsync(
                x => x.ProtectedSystemId == protectedSystemId &&
                     x.Code == code &&
                     x.Status == AlertStatus.Open,
                cancellationToken);

        if (exists)
        {
            return 0;
        }

        _dbContext.Alerts.Add(new Alert
        {
            ProtectedSystemId = protectedSystemId,
            Code = code,
            Title = title,
            Message = message,
            Severity = severity,
            Status = AlertStatus.Open
        });

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = protectedSystemId,
            EventType = "alert_created",
            Severity = severity,
            Message = $"Alert created: {code} - {title}"
        });

        return 1;
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpenAlerts(CancellationToken cancellationToken)
    {
        var alerts = await _dbContext.Alerts
            .AsNoTracking()
            .Include(x => x.ProtectedSystem)
                .ThenInclude(x => x!.Simulator)
            .Where(x => x.Status == Sbc.Domain.Enums.AlertStatus.Open)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Title,
                x.Message,
                x.Severity,
                x.Status,
                x.CreatedAtUtc,
                x.ResolvedAtUtc,
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

        return Ok(alerts);
    }
    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(
    Guid id,
    CancellationToken cancellationToken)
    {
        var alert = await _dbContext.Alerts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (alert is null)
        {
            return NotFound(new
            {
                Message = "Alert not found."
            });
        }

        if (alert.Status == Sbc.Domain.Enums.AlertStatus.Resolved)
        {
            return Ok(new
            {
                Message = "Alert is already resolved.",
                alert.Id,
                alert.Code,
                alert.Status,
                alert.ResolvedAtUtc
            });
        }

        alert.Status = Sbc.Domain.Enums.AlertStatus.Resolved;
        alert.ResolvedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Message = "Alert resolved successfully.",
            alert.Id,
            alert.Code,
            alert.Status,
            alert.ResolvedAtUtc
        });
    }
}