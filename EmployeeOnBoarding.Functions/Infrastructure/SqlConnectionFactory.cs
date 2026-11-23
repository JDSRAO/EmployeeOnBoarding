using System.Data;
using Microsoft.Data.SqlClient;

namespace EmployeeOnBoarding.Functions.Infrastructure;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
//This creates a connection to SQL Server using the configured connection string.
public sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
