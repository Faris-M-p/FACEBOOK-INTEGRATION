namespace FACEBOOK_INTEGRATION.Models;

public sealed record FacebookPage(
    int ClientId,
    string PageId,
    string PageName,
    string PageAccessToken,
    DateTimeOffset? LastSyncUtc
);

