using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/backup-events")]
public class BackupEventsController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public BackupEventsController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var events = await _dbContext.BackupEvents
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ProtectedSystemId,
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
                    },
                x.BackupJobId,
                BackupJob = x.BackupJob == null
                    ? null
                    : new
                    {
                        x.BackupJob.Id,
                        Source = x.BackupJob.Source.ToString(),
                        BackupType = x.BackupJob.BackupType.ToString(),
                        Status = x.BackupJob.Status.ToString()
                    },
                x.EventType,
                Severity = x.Severity.ToString(),
                x.Message,
                x.RawPayloadJson,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var backupEvent = await _dbContext.BackupEvents
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.ProtectedSystemId,
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
                    },
                x.BackupJobId,
                BackupJob = x.BackupJob == null
                    ? null
                    : new
                    {
                        x.BackupJob.Id,
                        Source = x.BackupJob.Source.ToString(),
                        BackupType = x.BackupJob.BackupType.ToString(),
                        Status = x.BackupJob.Status.ToString()
                    },
                x.EventType,
                Severity = x.Severity.ToString(),
                x.Message,
                x.RawPayloadJson,
                x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (backupEvent is null)
        {
            return NotFound();
        }

        return Ok(backupEvent);
    }
}