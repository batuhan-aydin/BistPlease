using Dapper;
using System.Data;

namespace BistPlease.Worker.Core.Data;

public abstract class BaseRepository
{
    protected IDbConnectionFactory ConnectionFactory;
    public BaseRepository(IDbConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory;
    }

    public virtual async Task<IEnumerable<T>> GetEnumerableAsync<T>(string sql, object? parameter = null)
    {
        using (IDbConnection connection = ConnectionFactory.CreateDbConnection(DatabaseConnection.BistDb))
        {
            try
            {
                connection.Open();
                return await connection.QueryAsync<T>(sql, parameter);
            }
            catch (Exception)
            {
                return Enumerable.Empty<T>();
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    public async virtual Task<T?> GetAsync<T>(string sql, object? parameter = null)
    {
        using (IDbConnection connection = ConnectionFactory.CreateDbConnection(DatabaseConnection.BistDb))
        {
            try
            {
                connection.Open();
                return await connection.QueryFirstOrDefaultAsync<T>(sql, parameter);
            }
            catch (Exception)
            {
                return default(T);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    public async virtual Task<int> ExecuteAsync(string sql, object? parameter = null)
    {
        using (IDbConnection connection = ConnectionFactory.CreateDbConnection(DatabaseConnection.BistDb))
        {
            try
            {
                connection.Open();
                return await connection.ExecuteAsync(sql, parameter);
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    public async virtual Task<int> ExecuteMultipleAsync(string sql, IEnumerable<object> parameters)
    {
        using (IDbConnection connection = ConnectionFactory.CreateDbConnection(DatabaseConnection.BistDb))
        {
            try
            {
                var result = 0;
                connection.Open();
                foreach(var parameter in parameters)
                {
                    result += await connection.ExecuteAsync(sql, parameter);
                }
                return result;
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }
}
