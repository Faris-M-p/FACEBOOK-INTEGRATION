using FACEBOOK_INTEGRATION.Interface;
using FACEBOOK_INTEGRATION.Models;
using FACEBOOK_INTEGRATION.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class FacebookPageRepository : IFacebookPageInterface
{
    private readonly AppDbContext _dbContext;

    public FacebookPageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<FacebookPage>> GetByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        return await _dbContext.FacebookPages
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .OrderBy(p => p.PageName)
            .Select(p => new FacebookPage(p.ClientId, p.PageId, p.PageName, p.PageAccessToken, p.LastSyncUtc))
            .ToListAsync(ct);
    }

    public async Task<FacebookPage?> GetByClientAndPageIdAsync(int clientId, string pageId, CancellationToken ct = default)
    {
        return await _dbContext.FacebookPages
            .AsNoTracking()
            .Where(p => p.ClientId == clientId && p.PageId == pageId)
            .Select(p => new FacebookPage(p.ClientId, p.PageId, p.PageName, p.PageAccessToken, p.LastSyncUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(FacebookPage page, CancellationToken ct = default)
    {
        var entity = await _dbContext.FacebookPages
            .FirstOrDefaultAsync(p => p.ClientId == page.ClientId && p.PageId == page.PageId, ct);

        if (entity is null)
        {
            entity = new FacebookPageEntity
            {
                ClientId = page.ClientId,
                PageId = page.PageId,
                PageName = page.PageName,
                PageAccessToken = page.PageAccessToken,
                LastSyncUtc = page.LastSyncUtc
            };
            _dbContext.FacebookPages.Add(entity);
        }
        else
        {
            entity.PageName = page.PageName;
            entity.PageAccessToken = page.PageAccessToken;
            entity.LastSyncUtc = page.LastSyncUtc;
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
