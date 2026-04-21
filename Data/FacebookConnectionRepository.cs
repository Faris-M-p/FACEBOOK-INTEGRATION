using FACEBOOK_INTEGRATION.Interface;
using FACEBOOK_INTEGRATION.Models;
using FACEBOOK_INTEGRATION.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class FacebookConnectionRepository : IFacebookConnectionInterface
{
    private readonly AppDbContext _dbContext;

    public FacebookConnectionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FacebookConnection?> GetByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        return await _dbContext.FacebookConnections
            .AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .Select(c => new FacebookConnection(c.ClientId, c.UserAccessToken, c.ExpiresAtUtc, c.LastSyncUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(FacebookConnection connection, CancellationToken ct = default)
    {
        var entity = await _dbContext.FacebookConnections
            .FirstOrDefaultAsync(c => c.ClientId == connection.ClientId, ct);

        if (entity is null)
        {
            entity = new FacebookConnectionEntity
            {
                ClientId = connection.ClientId,
                UserAccessToken = connection.UserAccessToken,
                ExpiresAtUtc = connection.ExpiresAtUtc,
                LastSyncUtc = connection.LastSyncUtc
            };
            _dbContext.FacebookConnections.Add(entity);
        }
        else
        {
            entity.UserAccessToken = connection.UserAccessToken;
            entity.ExpiresAtUtc = connection.ExpiresAtUtc;
            entity.LastSyncUtc = connection.LastSyncUtc;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateLastSyncAsync(int clientId, DateTimeOffset lastSyncUtc, CancellationToken ct = default)
    {
        var entity = await _dbContext.FacebookConnections
            .FirstOrDefaultAsync(c => c.ClientId == clientId, ct);

        if (entity is not null)
        {
            entity.LastSyncUtc = lastSyncUtc;
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
