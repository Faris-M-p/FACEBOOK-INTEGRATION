using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FACEBOOK_INTEGRATION.Data;
using FACEBOOK_INTEGRATION.Models;
using FACEBOOK_INTEGRATION.Options;
using Microsoft.Extensions.Options;

namespace FACEBOOK_INTEGRATION.Services;

public sealed class FacebookService
{
    private const string GraphVersion = "v19.0";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FacebookOptions _options;
    private readonly FacebookConnectionRepository _connectionRepo;
    private readonly FacebookPageRepository _pageRepo;
    private readonly FacebookPostRepository _postRepo;
    private readonly FacebookTokenService _tokenService;

    public FacebookService(
        IHttpClientFactory httpClientFactory,
        IOptions<FacebookOptions> options,
        FacebookConnectionRepository connectionRepo,
        FacebookPageRepository pageRepo,
        FacebookPostRepository postRepo,
        FacebookTokenService tokenService)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _connectionRepo = connectionRepo;
        _pageRepo = pageRepo;
        _postRepo = postRepo;
        _tokenService = tokenService;
    }

    public string BuildOAuthUrl(string state)
    {
        var scopes = "pages_show_list,pages_manage_posts,pages_read_engagement";
        var url =
            $"https://www.facebook.com/{GraphVersion}/dialog/oauth?client_id={Uri.EscapeDataString(_options.AppId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUrl)}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&scope={Uri.EscapeDataString(scopes)}";
        return url;
    }

    public async Task<(bool ok, string message)> ExchangeCodeAndSyncPagesAsync(int clientId, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret) || string.IsNullOrWhiteSpace(_options.RedirectUrl))
            return (false, "Facebook app is not configured (AppId/AppSecret/RedirectUrl).");

        var http = _httpClientFactory.CreateClient();

        var tokenUrl =
            $"https://graph.facebook.com/{GraphVersion}/oauth/access_token" +
            $"?client_id={Uri.EscapeDataString(_options.AppId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUrl)}" +
            $"&client_secret={Uri.EscapeDataString(_options.AppSecret)}" +
            $"&code={Uri.EscapeDataString(code)}";

        using var tokenResp = await http.GetAsync(tokenUrl, ct);
        var tokenBody = await tokenResp.Content.ReadAsStringAsync(ct);
        if (!tokenResp.IsSuccessStatusCode)
        {
            return (false, FacebookTokenService.TryGetGraphErrorMessage(tokenBody) ?? "OAuth token exchange failed");
        }

        using var tokenDoc = JsonDocument.Parse(tokenBody);
        var userToken = tokenDoc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var expiresIn = tokenDoc.RootElement.TryGetProperty("expires_in", out var ei) && ei.ValueKind == JsonValueKind.Number ? ei.GetInt32() : (int?)null;
        if (string.IsNullOrWhiteSpace(userToken))
            return (false, "OAuth token exchange failed (missing access_token).");

        var expiresAtUtc = expiresIn is null ? (DateTimeOffset?)null : DateTimeOffset.UtcNow.AddSeconds(expiresIn.Value);
        var now = DateTimeOffset.UtcNow;

        await _connectionRepo.UpsertAsync(new FacebookConnection(
            ClientId: clientId,
            UserAccessToken: userToken,
            ExpiresAtUtc: expiresAtUtc,
            LastSyncUtc: now
        ), ct);

        // Sync pages: /me/accounts
        var pagesUrl = $"https://graph.facebook.com/{GraphVersion}/me/accounts?access_token={Uri.EscapeDataString(userToken)}";
        using var pagesResp = await http.GetAsync(pagesUrl, ct);
        var pagesBody = await pagesResp.Content.ReadAsStringAsync(ct);
        if (!pagesResp.IsSuccessStatusCode)
        {
            return (false, FacebookTokenService.TryGetGraphErrorMessage(pagesBody) ?? "Failed to fetch pages");
        }

        using var pagesDoc = JsonDocument.Parse(pagesBody);
        if (!pagesDoc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return (false, "Failed to fetch pages (missing data).");

        foreach (var item in data.EnumerateArray())
        {
            var pageId = item.TryGetProperty("id", out var id) ? id.GetString() : null;
            var pageName = item.TryGetProperty("name", out var n) ? n.GetString() : null;
            var pageToken = item.TryGetProperty("access_token", out var pt) ? pt.GetString() : null;
            if (string.IsNullOrWhiteSpace(pageId) || string.IsNullOrWhiteSpace(pageName) || string.IsNullOrWhiteSpace(pageToken))
                continue;

            await _pageRepo.UpsertAsync(new FacebookPage(
                ClientId: clientId,
                PageId: pageId,
                PageName: pageName,
                PageAccessToken: pageToken,
                LastSyncUtc: now
            ), ct);
        }

        await _connectionRepo.UpdateLastSyncAsync(clientId, now, ct);
        return (true, "Facebook connected and pages synced.");
    }

    public Task<List<FacebookPage>> GetPagesAsync(int clientId, CancellationToken ct = default)
        => _pageRepo.GetByClientIdAsync(clientId, ct);

    public async Task<(bool ok, string message)> CanPostAsync(int clientId, string pageId, CancellationToken ct = default)
    {
        var connection = await _connectionRepo.GetByClientIdAsync(clientId, ct);
        if (connection is null) return (false, "Reconnect Facebook");

        if (_tokenService.IsTokenExpired(connection.ExpiresAtUtc, DateTimeOffset.UtcNow))
            return (false, "Reconnect Facebook");

        // Validate USER token (required to keep "connection status" honest)
        var userValid = await _tokenService.ValidateTokenAsync(connection.UserAccessToken, ct);
        if (!userValid.ok) return (false, "Reconnect Facebook");

        // Validate PAGE token (required before any post)
        var page = await _pageRepo.GetByClientAndPageIdAsync(clientId, pageId, ct);
        if (page is null) return (false, "Invalid page");

        var pageValid = await _tokenService.ValidateTokenAsync(page.PageAccessToken, ct);
        if (!pageValid.ok) return (false, "Reconnect Facebook");

        return (true, "OK");
    }

    public async Task<FacebookPostResult> PostTextAsync(int clientId, string pageId, string caption, CancellationToken ct = default)
    {
        var can = await CanPostAsync(clientId, pageId, ct);
        if (!can.ok) return new FacebookPostResult(false, can.message);

        var page = await _pageRepo.GetByClientAndPageIdAsync(clientId, pageId, ct);
        if (page is null) return new FacebookPostResult(false, "Invalid page");

        var http = _httpClientFactory.CreateClient();
        var url = $"https://graph.facebook.com/{GraphVersion}/{pageId}/feed";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["message"] = caption ?? "",
            ["access_token"] = page.PageAccessToken
        });

        using var resp = await http.PostAsync(url, content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            var msg = FacebookTokenService.MapGraphErrorToUserMessage(body);
            await _postRepo.InsertAsync(clientId, pageId, FacebookPostType.Text, caption, false, msg, null, ct);
            return new FacebookPostResult(false, msg);
        }

        var postId = TryReadId(body);
        await _postRepo.InsertAsync(clientId, pageId, FacebookPostType.Text, caption, true, "OK", postId, ct);
        return new FacebookPostResult(true, "Published", postId);
    }

    public async Task<FacebookPostResult> PostImageAsync(int clientId, string pageId, string caption, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var can = await CanPostAsync(clientId, pageId, ct);
        if (!can.ok) return new FacebookPostResult(false, can.message);

        var page = await _pageRepo.GetByClientAndPageIdAsync(clientId, pageId, ct);
        if (page is null) return new FacebookPostResult(false, "Invalid page");

        var http = _httpClientFactory.CreateClient();
        var url = $"https://graph.facebook.com/{GraphVersion}/{pageId}/photos";

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(page.PageAccessToken), "access_token");
        form.Add(new StringContent(caption ?? ""), "caption");

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(fileContent, "source", fileName);

        using var resp = await http.PostAsync(url, form, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            var msg = FacebookTokenService.MapGraphErrorToUserMessage(body);
            await _postRepo.InsertAsync(clientId, pageId, FacebookPostType.Image, caption, false, msg, null, ct);
            return new FacebookPostResult(false, msg);
        }

        var postId = TryReadId(body);
        await _postRepo.InsertAsync(clientId, pageId, FacebookPostType.Image, caption, true, "OK", postId, ct);
        return new FacebookPostResult(true, "Published", postId);
    }

    public async Task<FacebookPostResult> PostVideoAsync(int clientId, string pageId, string caption, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var can = await CanPostAsync(clientId, pageId, ct);
        if (!can.ok) return new FacebookPostResult(false, can.message);

        var page = await _pageRepo.GetByClientAndPageIdAsync(clientId, pageId, ct);
        if (page is null) return new FacebookPostResult(false, "Invalid page");

        var http = _httpClientFactory.CreateClient();
        var url = $"https://graph.facebook.com/{GraphVersion}/{pageId}/videos";

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(page.PageAccessToken), "access_token");
        form.Add(new StringContent(caption ?? ""), "description");

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(fileContent, "source", fileName);

        using var resp = await http.PostAsync(url, form, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            var msg = FacebookTokenService.MapGraphErrorToUserMessage(body);
            await _postRepo.InsertAsync(clientId, pageId, FacebookPostType.Video, caption, false, msg, null, ct);
            return new FacebookPostResult(false, msg);
        }

        var postId = TryReadId(body);
        await _postRepo.InsertAsync(clientId, pageId, FacebookPostType.Video, caption, true, "OK", postId, ct);
        return new FacebookPostResult(true, "Published", postId);
    }

    private static string? TryReadId(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                return id.GetString();
            if (doc.RootElement.TryGetProperty("post_id", out var pid) && pid.ValueKind == JsonValueKind.String)
                return pid.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }
}

