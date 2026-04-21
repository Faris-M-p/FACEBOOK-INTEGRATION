namespace FACEBOOK_INTEGRATION.Models.Entities;

public class FacebookPageEntity
{
    public int ClientId { get; set; }
    public string PageId { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string PageAccessToken { get; set; } = string.Empty;
    public DateTimeOffset? LastSyncUtc { get; set; }
}
