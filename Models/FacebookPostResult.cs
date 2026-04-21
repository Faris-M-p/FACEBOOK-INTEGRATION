namespace FACEBOOK_INTEGRATION.Models;

public sealed record FacebookPostResult(
    bool Ok,
    string Message,
    string? FacebookPostId = null
);

