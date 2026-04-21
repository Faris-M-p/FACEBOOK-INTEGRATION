using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Interface;

public interface IFacebookConnectionInterface
{
    Task<FacebookConnection?> GetByClientIdAsync(int clientId, CancellationToken ct = default);
    Task UpsertAsync(FacebookConnection connection, CancellationToken ct = default);
    Task UpdateLastSyncAsync(int clientId, DateTimeOffset lastSyncUtc, CancellationToken ct = default);
}
