using FACEBOOK_INTEGRATION.Extensions;
using FACEBOOK_INTEGRATION.Models;
using FACEBOOK_INTEGRATION.Services;
using Microsoft.AspNetCore.Mvc;

namespace FACEBOOK_INTEGRATION.Controllers;

[Route("facebook")]
public sealed class FacebookController : Controller
{
    private readonly FacebookService _facebook;

    public FacebookController(FacebookService facebook)
    {
        _facebook = facebook;
    }

    [HttpGet("connect")]
    public IActionResult Connect()
    {
        var clientId = HttpContext.Session.GetClientId();
        if (clientId is null) return Redirect("/Index?msg=Select%20a%20client%20first");

        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(SessionKeys.FacebookOAuthState, state);

        var url = _facebook.BuildOAuthUrl(state);
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? error_description, CancellationToken ct)
    {
        var clientId = HttpContext.Session.GetClientId();
        if (clientId is null) return Redirect("/Index?msg=Select%20a%20client%20first");

        if (!string.IsNullOrWhiteSpace(error))
            return Redirect($"/Facebook/Integration?msg={Uri.EscapeDataString("Facebook error: " + (error_description ?? error))}");

        if (string.IsNullOrWhiteSpace(code))
            return Redirect("/Facebook/Integration?msg=Missing%20code");

        var expectedState = HttpContext.Session.GetString(SessionKeys.FacebookOAuthState);
        if (string.IsNullOrWhiteSpace(expectedState) || !string.Equals(expectedState, state, StringComparison.Ordinal))
            return Redirect("/Facebook/Integration?msg=Invalid%20OAuth%20state");

        var (ok, message) = await _facebook.ExchangeCodeAndSyncPagesAsync(clientId.Value, code, ct);
        return Redirect($"/Facebook/Integration?msg={Uri.EscapeDataString(message)}&ok={(ok ? "1" : "0")}");
    }

    [HttpGet("pages")]
    public async Task<IActionResult> GetPages(CancellationToken ct)
    {
        var clientId = HttpContext.Session.GetClientId();
        if (clientId is null) return Unauthorized(new { message = "Select client first" });

        var pages = await _facebook.GetPagesAsync(clientId.Value, ct);
        return Ok(pages.Select(p => new { p.PageId, p.PageName }));
    }

    [HttpPost("post")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Post(
        [FromForm] string pageId,
        [FromForm] FacebookPostType postType,
        [FromForm] string caption,
        IFormFile? file,
        CancellationToken ct)
    {
        var clientId = HttpContext.Session.GetClientId();
        if (clientId is null) return Redirect("/Index?msg=Select%20a%20client%20first");

        if (string.IsNullOrWhiteSpace(pageId))
            return Redirect("/Facebook/Post?msg=Invalid%20page");

        caption ??= "";

        FacebookPostResult result;
        switch (postType)
        {
            case FacebookPostType.Text:
                result = await _facebook.PostTextAsync(clientId.Value, pageId, caption, ct);
                break;

            case FacebookPostType.Image:
            case FacebookPostType.Video:
                if (file is null || file.Length == 0)
                    return Redirect("/Facebook/Post?msg=File%20required");

                await using (var stream = file.OpenReadStream())
                {
                    result = postType == FacebookPostType.Image
                        ? await _facebook.PostImageAsync(clientId.Value, pageId, caption, stream, file.FileName, file.ContentType, ct)
                        : await _facebook.PostVideoAsync(clientId.Value, pageId, caption, stream, file.FileName, file.ContentType, ct);
                }
                break;

            default:
                return Redirect("/Facebook/Post?msg=Invalid%20post%20type");
        }

        var msg = result.Ok ? "Published" : result.Message;
        return Redirect($"/Facebook/Post?msg={Uri.EscapeDataString(msg)}&ok={(result.Ok ? "1" : "0")}&postId={Uri.EscapeDataString(result.FacebookPostId ?? "")}");
    }
}

