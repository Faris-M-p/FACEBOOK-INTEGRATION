using FACEBOOK_INTEGRATION.Data;
using FACEBOOK_INTEGRATION.Extensions;
using FACEBOOK_INTEGRATION.Models;
using FACEBOOK_INTEGRATION.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FACEBOOK_INTEGRATION.Pages.Facebook;

public sealed class IntegrationModel : PageModel
{
    private readonly FacebookConnectionRepository _connections;
    private readonly FacebookPageRepository _pages;
    private readonly FacebookTokenService _tokenService;

    public IntegrationModel(
        FacebookConnectionRepository connections,
        FacebookPageRepository pages,
        FacebookTokenService tokenService)
    {
        _connections = connections;
        _pages = pages;
        _tokenService = tokenService;
    }

    public int ClientId { get; private set; }
    public string ConnectionStatus { get; private set; } = "Not connected";
    public DateTimeOffset? LastSyncUtc { get; private set; }
    public int PagesCount { get; private set; }
    public bool TokenValid { get; private set; }
    public List<FacebookPage> Pages { get; private set; } = new();

    public async Task<IActionResult> OnGet(string? msg = null, CancellationToken ct = default)
    {
        ViewData["Msg"] = msg;

        var clientId = HttpContext.Session.GetClientId();
        if (clientId is null) return Redirect("/Index?msg=Select%20a%20client%20first");
        ClientId = clientId.Value;

        var conn = await _connections.GetByClientIdAsync(ClientId, ct);
        if (conn is null)
        {
            ConnectionStatus = "Not connected";
            TokenValid = false;
            LastSyncUtc = null;
            PagesCount = 0;
            Pages = new List<FacebookPage>();
            return Page();
        }

        LastSyncUtc = conn.LastSyncUtc;
        Pages = await _pages.GetByClientIdAsync(ClientId, ct);
        PagesCount = Pages.Count;

        if (_tokenService.IsTokenExpired(conn.ExpiresAtUtc, DateTimeOffset.UtcNow))
        {
            ConnectionStatus = "Reconnect Required";
            TokenValid = false;
            return Page();
        }

        var validation = await _tokenService.ValidateTokenAsync(conn.UserAccessToken, ct);
        TokenValid = validation.ok;
        ConnectionStatus = validation.ok ? "Connected" : "Reconnect Required";
        return Page();
    }
}

