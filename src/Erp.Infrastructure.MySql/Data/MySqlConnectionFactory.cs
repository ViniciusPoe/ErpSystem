using MySqlConnector;

namespace Erp.Infrastructure.MySql.Data;

public sealed class MySqlConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string ConnectionString => _connectionString;

    public MySqlConnection Create() => new(_connectionString);
}
