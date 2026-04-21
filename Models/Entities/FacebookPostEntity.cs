namespace FACEBOOK_INTEGRATION.Models.Entities;

public class FacebookPostEntity
{
    public long Id { get; set; }
    public int ClientId { get; set; }
    public string PageId { get; set; } = string.Empty;
    public string PostType { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FacebookPostId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}
