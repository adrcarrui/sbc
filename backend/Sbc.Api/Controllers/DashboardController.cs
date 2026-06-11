using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Domain.Enums;
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
        var totalSimulators = await _dbContext.Simulators
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalSystems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var activeSystems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(x => x.IsActive, cancellationToken);

        var onlineSystems = await _dbContext.ProtectedSystems
.AsNoTracking()
.CountAsync(x => x.IsActive && x.IsOnline, cancellationToken);

        var offlineSystems = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(x => x.IsActive && !x.IsOnline, cancellationToken);

        var inactiveSystems = totalSystems - activeSystems;

        var systemsWithUrBackupClient = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(
                x => x.UrBackupClientId != null || x.UrBackupClientName != null,
                cancellationToken);

        var systemsPendingValidation = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(
                x => x.BackupCapability == BackupCapability.PendingValidation,
                cancellationToken);

        var systemsWithFileBackupValidated = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(x => x.FileBackupValidated, cancellationToken);

        var systemsWithImageBackupValidated = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(x => x.ImageBackupValidated, cancellationToken);

        var systemsWithLiveBackupValidated = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(x => x.LiveBackupValidated, cancellationToken);

        var systemsRequiringManualBackup = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .CountAsync(
                x => x.BackupCapability == BackupCapability.ManualBackupRequired,
                cancellationToken);

        var openAlerts = await _dbContext.Alerts
            .AsNoTracking()
            .CountAsync(x => x.Status == AlertStatus.Open, cancellationToken);

        var criticalOpenAlerts = await _dbContext.Alerts
            .AsNoTracking()
            .CountAsync(
                x => x.Status == AlertStatus.Open &&
                     x.Severity == AlertSeverity.Critical,
                cancellationToken);

        var pendingManualBackupRequests = await _dbContext.ManualBackupRequests
            .AsNoTracking()
            .CountAsync(
                x => x.Status == ManualBackupRequestStatus.Pending ||
                     x.Status == ManualBackupRequestStatus.InProgress,
                cancellationToken);

        var successfulBackupJobs = await _dbContext.BackupJobs
            .AsNoTracking()
            .CountAsync(x => x.Status == BackupJobStatus.Success, cancellationToken);

        var failedBackupJobs = await _dbContext.BackupJobs
            .AsNoTracking()
            .CountAsync(x => x.Status == BackupJobStatus.Failed, cancellationToken);

        var runningBackupJobs = await _dbContext.BackupJobs
            .AsNoTracking()
            .CountAsync(x => x.Status == BackupJobStatus.Running, cancellationToken);

        return Ok(new
        {
            totalSimulators,
            totalSystems,
            activeSystems,
            inactiveSystems,
            onlineSystems,
            offlineSystems,
            systemsWithUrBackupClient,
            systemsPendingValidation,
            systemsWithFileBackupValidated,
            systemsWithImageBackupValidated,
            systemsWithLiveBackupValidated,
            systemsRequiringManualBackup,
            openAlerts,
            criticalOpenAlerts,
            pendingManualBackupRequests,
            backupJobs = new
            {
                successful = successfulBackupJobs,
                failed = failedBackupJobs,
                running = runningBackupJobs
            }
        });
    }
}