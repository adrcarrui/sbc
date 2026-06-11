using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly SbcDbContext _dbContext;

    public HealthController(SbcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return Ok(new
        {
            status = "ok",
            database = canConnect ? "connected" : "unavailable",
            timestampUtc = DateTime.UtcNow
        });
    }
}