using FACEBOOK_INTEGRATION.Extensions;
using FACEBOOK_INTEGRATION.Interface;
using FACEBOOK_INTEGRATION.Models;
using FACEBOOK_INTEGRATION.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FACEBOOK_INTEGRATION.Pages.Facebook;

public sealed class PostModel : PageModel
{
    private readonly IFacebookConnectionInterface _connections;
    private readonly IFacebookPageInterface _pages;
    private readonly FacebookTokenService _tokenService;

    public PostModel(
        IFacebookConnectionInterface connections,
        IFacebookPageInterface pages,
        FacebookTokenService tokenService)
    {
        _connections = connections;
        _pages = pages;
        _tokenService = tokenService;
    }

    public int ClientId { get; private set; }
    public List<FacebookPage> Pages { get; private set; } = new();
    public bool CanPost { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGet(string? msg = null, string? ok = null, string? postId = null, CancellationToken ct = default)
    {
        var clientId = HttpContext.Session.GetClientId();
        if (clientId is null) return Redirect("/Index?msg=Select%20a%20client%20first");
        ClientId = clientId.Value;

        StatusMessage = msg;
        if (!string.IsNullOrWhiteSpace(postId))
            StatusMessage = string.IsNullOrWhiteSpace(StatusMessage) ? $"FacebookPostId: {postId}" : $"{StatusMessage} (FacebookPostId: {postId})";

        Pages = await _pages.GetByClientIdAsync(ClientId, ct);

        var conn = await _connections.GetByClientIdAsync(ClientId, ct);
        if (conn is null)
        {
            CanPost = false;
            return Page();
        }

        if (_tokenService.IsTokenExpired(conn.ExpiresAtUtc, DateTimeOffset.UtcNow))
        {
            CanPost = false;
            return Page();
        }

        var validation = await _tokenService.ValidateTokenAsync(conn.UserAccessToken, ct);
        CanPost = validation.ok;
        return Page();
    }
}

