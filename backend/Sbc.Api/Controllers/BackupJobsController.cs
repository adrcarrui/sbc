using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/backup-jobs")]
public class BackupJobsController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public BackupJobsController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var backupJobs = await _dbContext.BackupJobs
            .AsNoTracking()
            .Include(x => x.ProtectedSystem)
            .ThenInclude(x => x.Simulator)
            .OrderByDescending(x => x.FinishedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ProtectedSystemId,
                ProtectedSystem = new
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
                },
                Source = x.Source.ToString(),
                BackupType = x.BackupType.ToString(),
                Status = x.Status.ToString(),
                x.StartedAtUtc,
                x.FinishedAtUtc,
                x.DurationSeconds,
                x.SizeBytes,
                x.UrBackupJobId,
                x.BackupPath,
                x.ErrorMessage,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(backupJobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var backupJob = await _dbContext.BackupJobs
            .AsNoTracking()
            .Include(x => x.ProtectedSystem)
            .ThenInclude(x => x.Simulator)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.ProtectedSystemId,
                ProtectedSystem = new
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
                },
                Source = x.Source.ToString(),
                BackupType = x.BackupType.ToString(),
                Status = x.Status.ToString(),
                x.StartedAtUtc,
                x.FinishedAtUtc,
                x.DurationSeconds,
                x.SizeBytes,
                x.UrBackupJobId,
                x.BackupPath,
                x.ErrorMessage,
                x.CreatedAtUtc,
                Events = x.Events
                    .OrderByDescending(e => e.CreatedAtUtc)
                    .Select(e => new
                    {
                        e.Id,
                        e.EventType,
                        Severity = e.Severity.ToString(),
                        e.Message,
                        e.CreatedAtUtc
                    })
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (backupJob is null)
        {
            return NotFound();
        }

        return Ok(backupJob);
    }
}