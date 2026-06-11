using Microsoft.AspNetCore.Mvc;
using Sbc.Application.Integrations.UrBackup;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/urbackup")]
public class UrBackupController : ControllerBase
{
    private readonly IUrBackupClient _urBackupClient;
    private readonly IUrBackupClientSyncService _clientSyncService;

    public UrBackupController(
        IUrBackupClient urBackupClient,
        IUrBackupClientSyncService clientSyncService)
    {
        _urBackupClient = urBackupClient;
        _clientSyncService = clientSyncService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await _urBackupClient.CheckHealthAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("raw-status")]
    public async Task<IActionResult> GetRawStatus(CancellationToken cancellationToken)
    {
        var result = await _urBackupClient.GetRawStatusAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("sync-clients")]
    public async Task<IActionResult> SyncClients(CancellationToken cancellationToken)
    {
        var result = await _clientSyncService.SyncClientsAsync(cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}