using Microsoft.Data.SqlClient;
using System.Collections.Frozen;
using System.Data;

namespace BistPlease.Worker.Core.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly FrozenDictionary<DatabaseConnection, string> _connections;
    public DbConnectionFactory(FrozenDictionary<DatabaseConnection, string> connections)
    {
        _connections = connections;
    }

    public IDbConnection CreateDbConnection(DatabaseConnection connection)
    {
        if (_connections.TryGetValue(connection, out string? connectionString) 
            && !string.IsNullOrWhiteSpace(connectionString))
        {
            return new SqlConnection(connectionString);
        }

        throw new ArgumentNullException();
    }

}

public interface IDbConnectionFactory
{
    IDbConnection CreateDbConnection(DatabaseConnection connection);
}