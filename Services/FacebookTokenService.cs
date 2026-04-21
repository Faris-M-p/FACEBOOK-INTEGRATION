using System.Net;
using System.Text.Json;
using FACEBOOK_INTEGRATION.Options;
using Microsoft.Extensions.Options;

namespace FACEBOOK_INTEGRATION.Services;

public sealed class FacebookTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FacebookOptions _options;

    public FacebookTokenService(IHttpClientFactory httpClientFactory, IOptions<FacebookOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public bool IsTokenExpired(DateTimeOffset? expiresAtUtc, DateTimeOffset nowUtc)
    {
        if (expiresAtUtc is null) return false; // unknown expiry -> rely on debug_token
        return expiresAtUtc.Value <= nowUtc.AddMinutes(2);
    }

    public async Task<(bool ok, string message, long? expiresAtUnix)> ValidateTokenAsync(string inputToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
            return (false, "Facebook app is not configured (AppId/AppSecret).", null);

        var appAccessToken = $"{_options.AppId}|{_options.AppSecret}";
        var url =
            $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(inputToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";

        var http = _httpClientFactory.CreateClient();
        using var resp = await http.GetAsync(url, ct);

        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            return (false, TryGetGraphErrorMessage(body) ?? $"Token validation failed ({(int)resp.StatusCode}).", null);
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data))
            return (false, "Token validation failed (missing data).", null);

        var isValid = data.TryGetProperty("is_valid", out var v) && v.ValueKind == JsonValueKind.True;
        var expiresAtUnix = data.TryGetProperty("expires_at", out var exp) && exp.ValueKind == JsonValueKind.Number
            ? exp.GetInt64()
            : (long?)null;

        if (!isValid)
        {
            var err = data.TryGetProperty("error", out var e) ? e.ToString() : null;
            return (false, string.IsNullOrWhiteSpace(err) ? "Reconnect Facebook" : $"Reconnect Facebook ({err})", expiresAtUnix);
        }

        return (true, "OK", expiresAtUnix);
    }

    public static string? TryGetGraphErrorMessage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("error", out var error)) return null;

            var msg = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            var code = error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : (int?)null;
            var type = error.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (code is not null || !string.IsNullOrWhiteSpace(type))
                return $"{msg} (code {code}, {type})";

            return msg;
        }
        catch
        {
            return null;
        }
    }

    public static string MapGraphErrorToUserMessage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("error", out var error)) return "Post failed";

            var code = error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : (int?)null;
            var subcode = error.TryGetProperty("error_subcode", out var sc) && sc.ValueKind == JsonValueKind.Number
                ? sc.GetInt32()
                : (int?)null;

            // Common token invalid/expired
            if (code == 190) return "Reconnect Facebook";

            // Common permissions issues
            if (code is 10 or 200 or 2500) return "Permission missing";

            // Fallback
            var msg = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            if (!string.IsNullOrWhiteSpace(msg))
                return $"Post failed: {msg}";

            if (subcode is not null) return $"Post failed (subcode {subcode})";
            return "Post failed";
        }
        catch
        {
            return "Post failed";
        }
    }
}

