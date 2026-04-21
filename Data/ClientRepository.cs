using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class ClientRepository
{
    private readonly SqlConnectionFactory _factory;

    public ClientRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Client>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);

        // The DB already exists and column names can vary (ClientId vs ClientID vs Id, Name vs ClientName).
        // We detect likely column names and alias them so the demo runs without forcing schema changes.
        var columns = await GetColumnsAsync(conn, "Clients", ct);
        var idCol = PickColumn(columns, "ClientId", "ClientID", "client_id", "Client_ID", "Id", "ID");
        var nameCol = PickColumn(columns, "Name", "ClientName", "Client_Name", "name");

        if (idCol is null || nameCol is null)
        {
            // Return empty list instead of crashing the landing page.
            // This makes the app "runnable" and the user can adjust mappings later if needed.
            return new List<Client>();
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT [{idCol}] AS ClientId, [{nameCol}] AS Name FROM [Clients] ORDER BY [{nameCol}]";

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<Client>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new Client(
                ClientId: r.GetInt32(0),
                Name: r.GetString(1)
            ));
        }
        return list;
    }

    private static async Task<HashSet<string>> GetColumnsAsync(Microsoft.Data.SqlClient.SqlConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT c.name
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name = @TableName";
        cmd.Parameters.AddWithValue("@TableName", tableName);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await r.ReadAsync(ct))
            set.Add(r.GetString(0));
        return set;
    }

    private static string? PickColumn(HashSet<string> columns, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            if (columns.Contains(c)) return c;
        }
        return null;
    }
}

