using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Sbc.Application.Integrations.UrBackup;

namespace Sbc.Infrastructure.Integrations.UrBackup;

public sealed class UrBackupClient : IUrBackupClient
{
    private readonly HttpClient _httpClient;
    private readonly UrBackupOptions _options;

    private string _session = string.Empty;
    private bool _loggedIn;

    public UrBackupClient(
        HttpClient httpClient,
        IOptions<UrBackupOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<UrBackupHealthResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var checkedAtUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return new UrBackupHealthResult(
                IsReachable: false,
                BaseUrl: string.Empty,
                StatusCode: null,
                ErrorMessage: "UrBackup BaseUrl is not configured.",
                CheckedAtUtc: checkedAtUtc);
        }

        try
        {
            using var response = await _httpClient.GetAsync("/", cancellationToken);

            return new UrBackupHealthResult(
                IsReachable: true,
                BaseUrl: _options.BaseUrl,
                StatusCode: (int)response.StatusCode,
                ErrorMessage: response.IsSuccessStatusCode
                    ? null
                    : $"UrBackup responded with HTTP {(int)response.StatusCode}. Server is reachable, but response is not successful.",
                CheckedAtUtc: checkedAtUtc);
        }
        catch (Exception ex)
        {
            return new UrBackupHealthResult(
                IsReachable: false,
                BaseUrl: _options.BaseUrl,
                StatusCode: null,
                ErrorMessage: ex.Message,
                CheckedAtUtc: checkedAtUtc);
        }
    }

    public async Task<UrBackupRawStatusResult> GetRawStatusAsync(CancellationToken cancellationToken)
    {
        var checkedAtUtc = DateTime.UtcNow;
        var apiUrl = BuildApiUrlForDisplay();

        try
        {
            var loggedIn = await LoginAsync(cancellationToken);

            if (!loggedIn)
            {
                return new UrBackupRawStatusResult(
                    Success: false,
                    ApiUrl: apiUrl,
                    RawJson: null,
                    ErrorMessage: "Could not log in to UrBackup. Check username/password and UrBackup permissions.",
                    CheckedAtUtc: checkedAtUtc);
            }

            var statusJson = await GetJsonAsync(
                action: "status",
                parameters: new Dictionary<string, string>(),
                includeSession: true,
                cancellationToken: cancellationToken);

            if (statusJson is null)
            {
                return new UrBackupRawStatusResult(
                    Success: false,
                    ApiUrl: apiUrl,
                    RawJson: null,
                    ErrorMessage: "UrBackup status response was empty or invalid.",
                    CheckedAtUtc: checkedAtUtc);
            }

            return new UrBackupRawStatusResult(
                Success: true,
                ApiUrl: apiUrl,
                RawJson: statusJson.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                }),
                ErrorMessage: null,
                CheckedAtUtc: checkedAtUtc);
        }
        catch (Exception ex)
        {
            return new UrBackupRawStatusResult(
                Success: false,
                ApiUrl: apiUrl,
                RawJson: null,
                ErrorMessage: ex.Message,
                CheckedAtUtc: checkedAtUtc);
        }
    }

    private async Task<bool> LoginAsync(CancellationToken cancellationToken)
    {
        if (_loggedIn)
        {
            return true;
        }

        var anonymousLogin = await GetJsonAsync(
            action: "login",
            parameters: new Dictionary<string, string>(),
            includeSession: false,
            cancellationToken: cancellationToken);

        if (IsSuccess(anonymousLogin))
        {
            _loggedIn = true;
            _session = anonymousLogin?["session"]?.GetValue<string>() ?? string.Empty;
            return true;
        }

        if (string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            return false;
        }

        var saltResponse = await GetJsonAsync(
            action: "salt",
            parameters: new Dictionary<string, string>
            {
                ["username"] = _options.Username
            },
            includeSession: false,
            cancellationToken: cancellationToken);

        if (saltResponse is null)
        {
            return false;
        }

        var session = saltResponse["ses"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(session))
        {
            return false;
        }

        _session = session;

        var salt = saltResponse["salt"]?.GetValue<string>();
        var random = saltResponse["rnd"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(salt) ||
            string.IsNullOrWhiteSpace(random))
        {
            return false;
        }

        var passwordHash = BuildUrBackupPasswordHash(
            password: _options.Password,
            salt: salt,
            random: random,
            pbkdf2Rounds: GetPbkdf2Rounds(saltResponse));

        var loginResponse = await GetJsonAsync(
            action: "login",
            parameters: new Dictionary<string, string>
            {
                ["username"] = _options.Username,
                ["password"] = passwordHash
            },
            includeSession: true,
            cancellationToken: cancellationToken);

        if (!IsSuccess(loginResponse))
        {
            return false;
        }

        _loggedIn = true;
        return true;
    }

    private async Task<JsonNode?> GetJsonAsync(
        string action,
        Dictionary<string, string> parameters,
        bool includeSession,
        CancellationToken cancellationToken)
    {
        if (includeSession && !string.IsNullOrWhiteSpace(_session))
        {
            parameters["ses"] = _session;
        }

        var requestUri = BuildRequestUri(action);
        var body = BuildFormUrlEncodedBody(parameters);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        request.Headers.Accept.ParseAdd("application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return JsonNode.Parse(content);
    }

    private string BuildRequestUri(string action)
    {
        var apiPath = string.IsNullOrWhiteSpace(_options.ApiPath)
            ? "/x"
            : _options.ApiPath;

        if (!apiPath.StartsWith('/'))
        {
            apiPath = "/" + apiPath;
        }

        return $"{apiPath}?a={Uri.EscapeDataString(action)}";
    }

    private string BuildApiUrlForDisplay()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var apiPath = string.IsNullOrWhiteSpace(_options.ApiPath)
            ? "/x"
            : _options.ApiPath;

        if (!apiPath.StartsWith('/'))
        {
            apiPath = "/" + apiPath;
        }

        return $"{baseUrl}{apiPath}";
    }

    private static string BuildFormUrlEncodedBody(Dictionary<string, string> parameters)
    {
        return string.Join(
            "&",
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }

    private static bool IsSuccess(JsonNode? node)
    {
        var successNode = node?["success"];

        return successNode is not null &&
               successNode.GetValue<bool>();
    }

    private static int GetPbkdf2Rounds(JsonNode saltResponse)
    {
        var roundsNode = saltResponse["pbkdf2_rounds"];

        if (roundsNode is null)
        {
            return 0;
        }

        if (roundsNode.GetValueKind() == JsonValueKind.Number)
        {
            return roundsNode.GetValue<int>();
        }

        var roundsText = roundsNode.GetValue<string>();

        return int.TryParse(roundsText, out var rounds)
            ? rounds
            : 0;
    }

    private static string BuildUrBackupPasswordHash(
        string password,
        string salt,
        string random,
        int pbkdf2Rounds)
    {
        var firstHashBytes = MD5.HashData(Encoding.UTF8.GetBytes(salt + password));

        var passwordHash = ToLowerHex(firstHashBytes);

        if (pbkdf2Rounds > 0)
        {
            var pbkdf2Bytes = Rfc2898DeriveBytes.Pbkdf2(
                password: firstHashBytes,
                salt: Encoding.UTF8.GetBytes(salt),
                iterations: pbkdf2Rounds,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);

            passwordHash = ToLowerHex(pbkdf2Bytes);
        }

        return Md5Hex(random + passwordHash);
    }

    private static string Md5Hex(string value)
    {
        return ToLowerHex(MD5.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string ToLowerHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}