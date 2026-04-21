using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Interface;

public interface IFacebookPageInterface
{
    Task<List<FacebookPage>> GetByClientIdAsync(int clientId, CancellationToken ct = default);
    Task<FacebookPage?> GetByClientAndPageIdAsync(int clientId, string pageId, CancellationToken ct = default);
    Task UpsertAsync(FacebookPage page, CancellationToken ct = default);
}
