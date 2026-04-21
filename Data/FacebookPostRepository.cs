using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class FacebookPostRepository
{
    private readonly SqlConnectionFactory _factory;

    public FacebookPostRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
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
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        // Expected:
        // FacebookPosts(ClientId int, PageId nvarchar(50), PostType nvarchar(20) or int, Caption nvarchar(max),
        //              Ok bit, Message nvarchar(max), FacebookPostId nvarchar(100) null, CreatedUtc datetimeoffset)
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO FacebookPosts (ClientId, PageId, PostType, Caption, Ok, Message, FacebookPostId, CreatedUtc)
VALUES (@ClientId, @PageId, @PostType, @Caption, @Ok, @Message, @FacebookPostId, @CreatedUtc)";
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        cmd.Parameters.AddWithValue("@PageId", pageId);
        cmd.Parameters.AddWithValue("@PostType", postType.ToString());
        cmd.Parameters.AddWithValue("@Caption", caption);
        cmd.Parameters.AddWithValue("@Ok", ok);
        cmd.Parameters.AddWithValue("@Message", message);
        cmd.Parameters.AddWithValue("@FacebookPostId", (object?)facebookPostId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedUtc", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

