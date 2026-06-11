namespace Sbc.Infrastructure.Integrations.UrBackup;

public sealed class UrBackupOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ApiPath { get; set; } = "/x";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;

    public int HealthCheckIntervalSeconds { get; set; } = 60;
}