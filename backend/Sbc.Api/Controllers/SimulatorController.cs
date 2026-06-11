using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Domain.Entities;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/simulators")]
public class SimulatorsController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public SimulatorsController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var simulators = await _dbContext.Simulators
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Location,
                SystemsCount = x.Systems.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(simulators);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var simulator = await _dbContext.Simulators
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Location,
                Systems = x.Systems
                    .OrderBy(system => system.Hostname)
                    .Select(system => new
                    {
                        system.Id,
                        system.Hostname,
                        system.IpAddress,
                        system.OperatingSystem,
                        Criticality = system.Criticality.ToString(),
                        BackupCapability = system.BackupCapability.ToString(),
                        system.IsActive
                    })
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (simulator is null)
        {
            return NotFound();
        }

        return Ok(simulator);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSimulatorRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest("Simulator code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Simulator name is required.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.Simulators
            .AnyAsync(x => x.Code == code, cancellationToken);

        if (exists)
        {
            return Conflict($"Simulator with code '{code}' already exists.");
        }

        var simulator = new Simulator
        {
            Code = code,
            Name = request.Name.Trim(),
            Location = NormalizeOptional(request.Location)
        };

        _dbContext.Simulators.Add(simulator);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = simulator.Id },
            new
            {
                simulator.Id,
                simulator.Code,
                simulator.Name,
                simulator.Location
            });
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

public sealed record CreateSimulatorRequest(
    string Code,
    string Name,
    string? Location);