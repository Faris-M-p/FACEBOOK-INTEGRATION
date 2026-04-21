using Microsoft.Data.SqlClient;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class SqlConnectionFactory
{
    private readonly IConfiguration _config;

    public SqlConnectionFactory(IConfiguration config)
    {
        _config = config;
    }

    public SqlConnection Create()
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

        return new SqlConnection(cs);
    }
}

