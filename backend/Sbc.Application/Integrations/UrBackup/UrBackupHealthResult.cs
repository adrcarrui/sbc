namespace Sbc.Application.Integrations.UrBackup;

public sealed record UrBackupHealthResult(
    bool IsReachable,
    string BaseUrl,
    int? StatusCode,
    string? ErrorMessage,
    DateTime CheckedAtUtc);
