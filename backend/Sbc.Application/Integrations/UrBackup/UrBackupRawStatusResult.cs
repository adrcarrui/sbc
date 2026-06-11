namespace Sbc.Application.Integrations.UrBackup;

public sealed record UrBackupRawStatusResult(
    bool Success,
    string ApiUrl,
    string? RawJson,
    string? ErrorMessage,
    DateTime CheckedAtUtc);