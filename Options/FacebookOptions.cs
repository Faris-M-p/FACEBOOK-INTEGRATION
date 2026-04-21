namespace FACEBOOK_INTEGRATION.Options;

public sealed class FacebookOptions
{
    public const string SectionName = "Facebook";

    public string AppId { get; set; } = "";
    public string AppSecret { get; set; } = "";
    public string RedirectUrl { get; set; } = "";
}

