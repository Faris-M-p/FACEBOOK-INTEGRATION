using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Interface;

public interface IFacebookPostInterface
{
    Task InsertAsync(
        int clientId,
        string pageId,
        FacebookPostType postType,
        string caption,
        bool ok,
        string message,
        string? facebookPostId,
        CancellationToken ct = default);
}
