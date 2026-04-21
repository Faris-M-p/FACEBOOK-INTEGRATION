using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class FacebookPageRepository
{
    private readonly SqlConnectionFactory _factory;

    public FacebookPageRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<FacebookPage>> GetByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        // Expected:
        // FacebookPages(ClientId int, PageId nvarchar(50), PageName nvarchar(200), PageAccessToken nvarchar(max), LastSyncUtc datetimeoffset null)
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT ClientId, PageId, PageName, PageAccessToken, LastSyncUtc
FROM FacebookPages
WHERE ClientId = @ClientId
ORDER BY PageName";
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<FacebookPage>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new FacebookPage(
                ClientId: r.GetInt32(0),
                PageId: r.GetString(1),
                PageName: r.GetString(2),
                PageAccessToken: r.GetString(3),
                LastSyncUtc: r.IsDBNull(4) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(4)
            ));
        }
        return list;
    }

    public async Task<FacebookPage?> GetByClientAndPageIdAsync(int clientId, string pageId, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT ClientId, PageId, PageName, PageAccessToken, LastSyncUtc
FROM FacebookPages
WHERE ClientId = @ClientId AND PageId = @PageId";
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        cmd.Parameters.AddWithValue("@PageId", pageId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new FacebookPage(
            ClientId: r.GetInt32(0),
            PageId: r.GetString(1),
            PageName: r.GetString(2),
            PageAccessToken: r.GetString(3),
            LastSyncUtc: r.IsDBNull(4) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(4)
        );
    }

    public async Task UpsertAsync(FacebookPage page, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM FacebookPages WHERE ClientId = @ClientId AND PageId = @PageId)
BEGIN
  UPDATE FacebookPages
  SET PageName = @PageName,
      PageAccessToken = @PageAccessToken,
      LastSyncUtc = @LastSyncUtc
  WHERE ClientId = @ClientId AND PageId = @PageId
END
ELSE
BEGIN
  INSERT INTO FacebookPages (ClientId, PageId, PageName, PageAccessToken, LastSyncUtc)
  VALUES (@ClientId, @PageId, @PageName, @PageAccessToken, @LastSyncUtc)
END";
        cmd.Parameters.AddWithValue("@ClientId", page.ClientId);
        cmd.Parameters.AddWithValue("@PageId", page.PageId);
        cmd.Parameters.AddWithValue("@PageName", page.PageName);
        cmd.Parameters.AddWithValue("@PageAccessToken", page.PageAccessToken);
        cmd.Parameters.AddWithValue("@LastSyncUtc", (object?)page.LastSyncUtc ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

