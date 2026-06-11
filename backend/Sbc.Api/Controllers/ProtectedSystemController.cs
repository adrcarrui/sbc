using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Domain.Entities;
using Sbc.Domain.Enums;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/protected-systems")]
public class ProtectedSystemsController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public ProtectedSystemsController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
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
                x.FileSystem,
                x.PartitionScheme,
                x.UrBackupClientId,
                x.UrBackupClientName,
                x.UrBackupClientVersion,
                Criticality = x.Criticality.ToString(),
                BackupCapability = x.BackupCapability.ToString(),
                x.FileBackupValidated,
                x.ImageBackupValidated,
                x.LiveBackupValidated,
                x.IsOnline,
                x.LastSeenAtUtc,
                x.LastFileBackupAtUtc,
                x.LastImageBackupAtUtc,
                x.LastFileBackupOk,
                x.LastImageBackupOk,
                x.LastFileBackupIssues,
                x.UrBackupStatusCode,
                x.IsActive,
                x.Notes,
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var system = await _dbContext.ProtectedSystems
            .AsNoTracking()
            .Include(x => x.Simulator)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Hostname,
                x.IpAddress,
                x.OperatingSystem,
                x.FileSystem,
                x.PartitionScheme,
                x.UrBackupClientId,
                x.UrBackupClientName,
                x.UrBackupClientVersion,
                Criticality = x.Criticality.ToString(),
                BackupCapability = x.BackupCapability.ToString(),
                x.FileBackupValidated,
                x.ImageBackupValidated,
                x.LiveBackupValidated,
                x.IsOnline,
                x.LastSeenAtUtc,
                x.LastFileBackupAtUtc,
                x.LastImageBackupAtUtc,
                x.LastFileBackupOk,
                x.LastImageBackupOk,
                x.LastFileBackupIssues,
                x.UrBackupStatusCode,
                x.IsActive,
                x.Notes,
                Simulator = x.Simulator == null
        ? null
        : new
        {
            x.Simulator.Id,
            x.Simulator.Code,
            x.Simulator.Name
        }
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (system is null)
        {
            return NotFound();
        }

        return Ok(system);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProtectedSystemRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Hostname))
        {
            return BadRequest("Hostname is required.");
        }

        if (request.SimulatorId is not null)
        {
            var simulatorExists = await _dbContext.Simulators
                .AnyAsync(x => x.Id == request.SimulatorId, cancellationToken);

            if (!simulatorExists)
            {
                return BadRequest("The selected simulator does not exist.");
            }
        }

        var hostname = request.Hostname.Trim();

        var exists = await _dbContext.ProtectedSystems
            .AnyAsync(x => x.Hostname == hostname, cancellationToken);

        if (exists)
        {
            return Conflict($"Protected system with hostname '{hostname}' already exists.");
        }

        var protectedSystem = new ProtectedSystem
        {
            SimulatorId = request.SimulatorId,
            Hostname = hostname,
            IpAddress = NormalizeOptional(request.IpAddress),
            OperatingSystem = NormalizeOptional(request.OperatingSystem),
            FileSystem = NormalizeOptional(request.FileSystem),
            PartitionScheme = NormalizeOptional(request.PartitionScheme),
            UrBackupClientId = NormalizeOptional(request.UrBackupClientId),
            UrBackupClientName = NormalizeOptional(request.UrBackupClientName),
            UrBackupClientVersion = NormalizeOptional(request.UrBackupClientVersion),
            Criticality = request.Criticality ?? Criticality.Medium,
            BackupCapability = request.BackupCapability ?? BackupCapability.PendingValidation,
            FileBackupValidated = request.FileBackupValidated,
            ImageBackupValidated = request.ImageBackupValidated,
            LiveBackupValidated = request.LiveBackupValidated,
            IsActive = true,
            Notes = NormalizeOptional(request.Notes)
        };

        _dbContext.ProtectedSystems.Add(protectedSystem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = protectedSystem.Id },
            new
            {
                protectedSystem.Id,
                protectedSystem.Hostname,
                protectedSystem.IpAddress,
                protectedSystem.OperatingSystem,
                Criticality = protectedSystem.Criticality.ToString(),
                BackupCapability = protectedSystem.BackupCapability.ToString()
            });
    }
    [HttpGet("{id:guid}/backup-jobs")]
    public async Task<IActionResult> GetBackupJobsBySystem(
        Guid id,
        CancellationToken cancellationToken)
    {
        var systemExists = await _dbContext.ProtectedSystems
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!systemExists)
        {
            return NotFound();
        }

        var backupJobs = await _dbContext.BackupJobs
            .AsNoTracking()
            .Where(x => x.ProtectedSystemId == id)
            .OrderByDescending(x => x.FinishedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
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

    [HttpGet("{id:guid}/backup-events")]
    public async Task<IActionResult> GetBackupEventsBySystem(
    Guid id,
    CancellationToken cancellationToken)
    {
        var systemExists = await _dbContext.ProtectedSystems
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!systemExists)
        {
            return NotFound();
        }

        var events = await _dbContext.BackupEvents
            .AsNoTracking()
            .Where(x => x.ProtectedSystemId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.BackupJobId,
                x.EventType,
                Severity = x.Severity.ToString(),
                x.Message,
                x.RawPayloadJson,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(events);
    }
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

public sealed record CreateProtectedSystemRequest(
    Guid? SimulatorId,
    string Hostname,
    string? IpAddress,
    string? OperatingSystem,
    string? FileSystem,
    string? PartitionScheme,
    string? UrBackupClientId,
    string? UrBackupClientName,
    string? UrBackupClientVersion,
    Criticality? Criticality,
    BackupCapability? BackupCapability,
    bool FileBackupValidated,
    bool ImageBackupValidated,
    bool LiveBackupValidated,
    string? Notes);