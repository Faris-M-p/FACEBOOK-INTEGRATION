namespace FACEBOOK_INTEGRATION.Models;

public sealed record FacebookConnection(
    int ClientId,
    string UserAccessToken,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastSyncUtc
);

