using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Domain.Entities;
using Sbc.Domain.Enums;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/manual-backup-requests")]
public class ManualBackupRequestsController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public ManualBackupRequestsController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var requests = await _dbContext.ManualBackupRequests
            .AsNoTracking()
            .Include(x => x.ProtectedSystem)
            .ThenInclude(x => x.Simulator)
            .OrderByDescending(x => x.RequestedAtUtc)
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
                x.RequestedBy,
                x.AssignedTo,
                x.Reason,
                x.RelatedChangeReference,
                Status = x.Status.ToString(),
                x.RequestedAtUtc,
                x.CompletedAtUtc,
                x.ValidatedBy,
                x.ValidatedAtUtc,
                x.ValidationNotes
            })
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.ManualBackupRequests
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
                x.RequestedBy,
                x.AssignedTo,
                x.Reason,
                x.RelatedChangeReference,
                Status = x.Status.ToString(),
                x.RequestedAtUtc,
                x.CompletedAtUtc,
                x.ValidatedBy,
                x.ValidatedAtUtc,
                x.ValidationNotes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        return Ok(request);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateManualBackupRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProtectedSystemId == Guid.Empty)
        {
            return BadRequest("Protected system id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            return BadRequest("Requested by is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest("Reason is required.");
        }

        var protectedSystemExists = await _dbContext.ProtectedSystems
            .AnyAsync(x => x.Id == request.ProtectedSystemId, cancellationToken);

        if (!protectedSystemExists)
        {
            return BadRequest("The selected protected system does not exist.");
        }

        var manualRequest = new ManualBackupRequest
        {
            ProtectedSystemId = request.ProtectedSystemId,
            RequestedBy = request.RequestedBy.Trim(),
            AssignedTo = NormalizeOptional(request.AssignedTo),
            Reason = request.Reason.Trim(),
            RelatedChangeReference = NormalizeOptional(request.RelatedChangeReference),
            Status = ManualBackupRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };

        _dbContext.ManualBackupRequests.Add(manualRequest);

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = request.ProtectedSystemId,
            EventType = "manual_backup_requested",
            Severity = AlertSeverity.Info,
            Message = $"Manual backup requested by {manualRequest.RequestedBy}. Reason: {manualRequest.Reason}"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = manualRequest.Id },
            new
            {
                manualRequest.Id,
                manualRequest.ProtectedSystemId,
                manualRequest.RequestedBy,
                manualRequest.AssignedTo,
                manualRequest.Reason,
                manualRequest.RelatedChangeReference,
                Status = manualRequest.Status.ToString(),
                manualRequest.RequestedAtUtc
            });
    }

    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignManualBackupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AssignedTo))
        {
            return BadRequest("Assigned to is required.");
        }

        var manualRequest = await _dbContext.ManualBackupRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (manualRequest is null)
        {
            return NotFound();
        }

        if (manualRequest.Status is ManualBackupRequestStatus.Validated
            or ManualBackupRequestStatus.Rejected
            or ManualBackupRequestStatus.Cancelled)
        {
            return Conflict("This manual backup request is already closed.");
        }

        manualRequest.AssignedTo = request.AssignedTo.Trim();

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = manualRequest.ProtectedSystemId,
            EventType = "manual_backup_assigned",
            Severity = AlertSeverity.Info,
            Message = $"Manual backup request assigned to {manualRequest.AssignedTo}."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var manualRequest = await _dbContext.ManualBackupRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (manualRequest is null)
        {
            return NotFound();
        }

        if (manualRequest.Status != ManualBackupRequestStatus.Pending)
        {
            return Conflict("Only pending manual backup requests can be started.");
        }

        manualRequest.Status = ManualBackupRequestStatus.InProgress;

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = manualRequest.ProtectedSystemId,
            EventType = "manual_backup_started",
            Severity = AlertSeverity.Info,
            Message = "Manual backup request started."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteManualBackupRequest request,
        CancellationToken cancellationToken)
    {
        var manualRequest = await _dbContext.ManualBackupRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (manualRequest is null)
        {
            return NotFound();
        }

        if (manualRequest.Status is ManualBackupRequestStatus.Validated
            or ManualBackupRequestStatus.Rejected
            or ManualBackupRequestStatus.Cancelled)
        {
            return Conflict("This manual backup request is already closed.");
        }

        manualRequest.Status = ManualBackupRequestStatus.Completed;
        manualRequest.CompletedAtUtc = DateTime.UtcNow;
        manualRequest.ValidationNotes = NormalizeOptional(request.CompletionNotes);

        _dbContext.BackupJobs.Add(new BackupJob
        {
            ProtectedSystemId = manualRequest.ProtectedSystemId,
            Source = BackupSource.Manual,
            BackupType = request.BackupType ?? BackupType.ManualOther,
            Status = BackupJobStatus.PendingValidation,
            StartedAtUtc = request.StartedAtUtc,
            FinishedAtUtc = DateTime.UtcNow,
            BackupPath = NormalizeOptional(request.BackupPath),
            ErrorMessage = null
        });

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = manualRequest.ProtectedSystemId,
            EventType = "manual_backup_completed",
            Severity = AlertSeverity.Info,
            Message = "Manual backup request completed and pending validation."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/validate")]
    public async Task<IActionResult> Validate(
        Guid id,
        [FromBody] ValidateManualBackupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ValidatedBy))
        {
            return BadRequest("Validated by is required.");
        }

        var manualRequest = await _dbContext.ManualBackupRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (manualRequest is null)
        {
            return NotFound();
        }

        if (manualRequest.Status != ManualBackupRequestStatus.Completed)
        {
            return Conflict("Only completed manual backup requests can be validated.");
        }

        manualRequest.Status = ManualBackupRequestStatus.Validated;
        manualRequest.ValidatedBy = request.ValidatedBy.Trim();
        manualRequest.ValidatedAtUtc = DateTime.UtcNow;
        manualRequest.ValidationNotes = NormalizeOptional(request.ValidationNotes);

        var latestPendingManualBackup = await _dbContext.BackupJobs
            .Where(x =>
                x.ProtectedSystemId == manualRequest.ProtectedSystemId &&
                x.Source == BackupSource.Manual &&
                x.Status == BackupJobStatus.PendingValidation)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestPendingManualBackup is not null)
        {
            latestPendingManualBackup.Status = BackupJobStatus.Success;
        }

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = manualRequest.ProtectedSystemId,
            EventType = "manual_backup_validated",
            Severity = AlertSeverity.Info,
            Message = $"Manual backup validated by {manualRequest.ValidatedBy}."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectManualBackupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest("Reject reason is required.");
        }

        var manualRequest = await _dbContext.ManualBackupRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (manualRequest is null)
        {
            return NotFound();
        }

        if (manualRequest.Status is ManualBackupRequestStatus.Validated
            or ManualBackupRequestStatus.Cancelled)
        {
            return Conflict("This manual backup request cannot be rejected.");
        }

        manualRequest.Status = ManualBackupRequestStatus.Rejected;
        manualRequest.ValidationNotes = request.Reason.Trim();

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = manualRequest.ProtectedSystemId,
            EventType = "manual_backup_rejected",
            Severity = AlertSeverity.Warning,
            Message = $"Manual backup rejected. Reason: {manualRequest.ValidationNotes}"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var manualRequest = await _dbContext.ManualBackupRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (manualRequest is null)
        {
            return NotFound();
        }

        if (manualRequest.Status is ManualBackupRequestStatus.Validated
            or ManualBackupRequestStatus.Rejected
            or ManualBackupRequestStatus.Cancelled)
        {
            return Conflict("This manual backup request is already closed.");
        }

        manualRequest.Status = ManualBackupRequestStatus.Cancelled;

        _dbContext.BackupEvents.Add(new BackupEvent
        {
            ProtectedSystemId = manualRequest.ProtectedSystemId,
            EventType = "manual_backup_cancelled",
            Severity = AlertSeverity.Warning,
            Message = "Manual backup request cancelled."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

public sealed record CreateManualBackupRequest(
    Guid ProtectedSystemId,
    string RequestedBy,
    string? AssignedTo,
    string Reason,
    string? RelatedChangeReference);

public sealed record AssignManualBackupRequest(
    string AssignedTo);

public sealed record CompleteManualBackupRequest(
    BackupType? BackupType,
    DateTime? StartedAtUtc,
    string? BackupPath,
    string? CompletionNotes);

public sealed record ValidateManualBackupRequest(
    string ValidatedBy,
    string? ValidationNotes);

public sealed record RejectManualBackupRequest(
    string Reason);