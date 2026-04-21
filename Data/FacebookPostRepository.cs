using FACEBOOK_INTEGRATION.Interface;
using FACEBOOK_INTEGRATION.Models;
using FACEBOOK_INTEGRATION.Models.Entities;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class FacebookPostRepository : IFacebookPostInterface
{
    private readonly AppDbContext _dbContext;

    public FacebookPostRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InsertAsync(
        int clientId,
        string pageId,
        FacebookPostType postType,
        string caption,
        bool ok,
        string message,
        string? facebookPostId,
        CancellationToken ct = default)
    {
        var entity = new FacebookPostEntity
        {
            ClientId = clientId,
            PageId = pageId,
            PostType = postType.ToString(),
            Caption = caption,
            Ok = ok,
            Message = message,
            FacebookPostId = facebookPostId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        _dbContext.FacebookPosts.Add(entity);
        await _dbContext.SaveChangesAsync(ct);
    }
}
