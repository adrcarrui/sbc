using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sbc.Application.Integrations.UrBackup;
using Sbc.Domain.Entities;
using Sbc.Domain.Enums;
using Sbc.Infrastructure.Persistence;

namespace Sbc.Api.Controllers;

[ApiController]
[Route("api/urbackup")]
public class UrBackupController : ControllerBase
{
    private readonly IUrBackupClient _urBackupClient;
    private readonly SbcDbContext _dbContext;

    public UrBackupController(
        IUrBackupClient urBackupClient,
        SbcDbContext dbContext)
    {
        _urBackupClient = urBackupClient;
        _dbContext = dbContext;
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
        var rawStatus = await _urBackupClient.GetRawStatusAsync(cancellationToken);

        if (!rawStatus.Success || string.IsNullOrWhiteSpace(rawStatus.RawJson))
        {
            return BadRequest(new
            {
                success = false,
                message = "Could not retrieve UrBackup status.",
                rawStatus.ErrorMessage
            });
        }

        var root = JsonNode.Parse(rawStatus.RawJson);
        var statusArray = root?["status"]?.AsArray();

        if (statusArray is null)
        {
            return BadRequest(new
            {
                success = false,
                message = "UrBackup status response does not contain a valid 'status' array."
            });
        }

        var createdClients = 0;
        var updatedClients = 0;
        var skippedClients = 0;

        var syncedClients = new List<object>();

        foreach (var clientNode in statusArray)
        {
            if (clientNode is null)
            {
                skippedClients++;
                continue;
            }

            var clientId = GetString(clientNode, "id");
            var clientName = NormalizeOptional(GetString(clientNode, "name"));

            if (string.IsNullOrWhiteSpace(clientName))
            {
                skippedClients++;
                continue;
            }

            var existingSystem = await FindExistingProtectedSystemAsync(
                clientId,
                clientName,
                cancellationToken);

            var wasCreated = false;

            if (existingSystem is null)
            {
                existingSystem = new ProtectedSystem
                {
                    Hostname = clientName,
                    Criticality = Criticality.Medium,
                    BackupCapability = BackupCapability.PendingValidation,
                    IsActive = true
                };

                _dbContext.ProtectedSystems.Add(existingSystem);
                wasCreated = true;
                createdClients++;
            }
            else
            {
                updatedClients++;
            }

            ApplyUrBackupClientStatus(existingSystem, clientNode, clientId, clientName);

            if (wasCreated)
            {
                _dbContext.BackupEvents.Add(new BackupEvent
                {
                    ProtectedSystem = existingSystem,
                    EventType = "urbackup_client_discovered",
                    Severity = AlertSeverity.Info,
                    Message = $"UrBackup client discovered: {clientName}."
                });
            }

            syncedClients.Add(new
            {
                urBackupClientId = clientId,
                name = clientName,
                online = existingSystem.IsOnline,
                operatingSystem = existingSystem.OperatingSystem,
                lastSeenAtUtc = existingSystem.LastSeenAtUtc,
                lastFileBackupAtUtc = existingSystem.LastFileBackupAtUtc,
                lastImageBackupAtUtc = existingSystem.LastImageBackupAtUtc,
                fileBackupOk = existingSystem.LastFileBackupOk,
                imageBackupOk = existingSystem.LastImageBackupOk,
                action = wasCreated ? "created" : "updated"
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            discoveredClients = statusArray.Count,
            createdClients,
            updatedClients,
            skippedClients,
            syncedClients
        });
    }

    private async Task<ProtectedSystem?> FindExistingProtectedSystemAsync(
        string? clientId,
        string clientName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var byClientId = await _dbContext.ProtectedSystems
                .FirstOrDefaultAsync(
                    x => x.UrBackupClientId == clientId,
                    cancellationToken);

            if (byClientId is not null)
            {
                return byClientId;
            }
        }

        var byClientName = await _dbContext.ProtectedSystems
            .FirstOrDefaultAsync(
                x => x.UrBackupClientName == clientName ||
                     x.Hostname == clientName,
                cancellationToken);

        return byClientName;
    }

    private static void ApplyUrBackupClientStatus(
        ProtectedSystem protectedSystem,
        JsonNode clientNode,
        string? clientId,
        string clientName)
    {
        var ipAddress = NormalizeOptional(GetString(clientNode, "ip"));

        if (ipAddress == "-")
        {
            ipAddress = null;
        }

        var osVersion = NormalizeOptional(GetString(clientNode, "os_version_string"));
        var osSimple = NormalizeOptional(GetString(clientNode, "os_simple"));

        protectedSystem.Hostname = clientName;
        protectedSystem.UrBackupClientId = clientId;
        protectedSystem.UrBackupClientName = clientName;
        protectedSystem.UrBackupClientVersion = NormalizeOptional(GetString(clientNode, "client_version_string"));
        protectedSystem.IpAddress = ipAddress;
        protectedSystem.OperatingSystem = osVersion ?? osSimple;

        protectedSystem.IsOnline = GetBool(clientNode, "online");
        protectedSystem.LastSeenAtUtc = FromUnixSeconds(GetLong(clientNode, "lastseen"));
        protectedSystem.LastFileBackupAtUtc = FromUnixSeconds(GetLong(clientNode, "lastbackup"));
        protectedSystem.LastImageBackupAtUtc = FromUnixSeconds(GetLong(clientNode, "lastbackup_image"));
        protectedSystem.LastFileBackupOk = GetBool(clientNode, "file_ok");
        protectedSystem.LastImageBackupOk = GetBool(clientNode, "image_ok");
        protectedSystem.LastFileBackupIssues = GetInt(clientNode, "last_filebackup_issues");
        protectedSystem.UrBackupStatusCode = GetInt(clientNode, "status");
        protectedSystem.IsActive = true;
    }

    private static string? GetString(JsonNode node, string propertyName)
    {
        var value = node[propertyName];

        return value is null
            ? null
            : value.ToString();
    }

    private static bool GetBool(JsonNode node, string propertyName)
    {
        var value = node[propertyName];

        if (value is null)
        {
            return false;
        }

        return value.GetValue<bool>();
    }

    private static long? GetLong(JsonNode node, string propertyName)
    {
        var value = node[propertyName];

        if (value is null)
        {
            return null;
        }

        if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static int? GetInt(JsonNode node, string propertyName)
    {
        var value = GetLong(node, propertyName);

        if (value is null)
        {
            return null;
        }

        if (value > int.MaxValue || value < int.MinValue)
        {
            return null;
        }

        return (int)value.Value;
    }

    private static DateTime? FromUnixSeconds(long? unixSeconds)
    {
        if (unixSeconds is null || unixSeconds <= 0)
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}