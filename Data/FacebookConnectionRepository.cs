using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class FacebookConnectionRepository
{
    private readonly SqlConnectionFactory _factory;

    public FacebookConnectionRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<FacebookConnection?> GetByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        // Expected:
        // FacebookConnections(ClientId int PK/FK, UserAccessToken nvarchar(max), ExpiresAtUtc datetimeoffset null, LastSyncUtc datetimeoffset null)
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT ClientId, UserAccessToken, ExpiresAtUtc, LastSyncUtc
FROM FacebookConnections
WHERE ClientId = @ClientId";
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new FacebookConnection(
            ClientId: r.GetInt32(0),
            UserAccessToken: r.GetString(1),
            ExpiresAtUtc: r.IsDBNull(2) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(2),
            LastSyncUtc: r.IsDBNull(3) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(3)
        );
    }

    public async Task UpsertAsync(FacebookConnection connection, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM FacebookConnections WHERE ClientId = @ClientId)
BEGIN
  UPDATE FacebookConnections
  SET UserAccessToken = @UserAccessToken,
      ExpiresAtUtc = @ExpiresAtUtc,
      LastSyncUtc = @LastSyncUtc
  WHERE ClientId = @ClientId
END
ELSE
BEGIN
  INSERT INTO FacebookConnections (ClientId, UserAccessToken, ExpiresAtUtc, LastSyncUtc)
  VALUES (@ClientId, @UserAccessToken, @ExpiresAtUtc, @LastSyncUtc)
END";
        cmd.Parameters.AddWithValue("@ClientId", connection.ClientId);
        cmd.Parameters.AddWithValue("@UserAccessToken", connection.UserAccessToken);
        cmd.Parameters.AddWithValue("@ExpiresAtUtc", (object?)connection.ExpiresAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastSyncUtc", (object?)connection.LastSyncUtc ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateLastSyncAsync(int clientId, DateTimeOffset lastSyncUtc, CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE FacebookConnections SET LastSyncUtc = @LastSyncUtc WHERE ClientId = @ClientId";
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        cmd.Parameters.AddWithValue("@LastSyncUtc", lastSyncUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

