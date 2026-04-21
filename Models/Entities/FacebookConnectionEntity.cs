namespace FACEBOOK_INTEGRATION.Models.Entities;

public class FacebookConnectionEntity
{
    public int ClientId { get; set; }
    public string UserAccessToken { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? LastSyncUtc { get; set; }
}
