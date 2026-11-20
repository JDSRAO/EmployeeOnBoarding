using Dapper;
using EmployeeOnBoarding.Api.Infrastructure;
using EmployeeOnBoarding.Api.Models;

namespace EmployeeOnBoarding.Api.Repositories;

public sealed class EmployeeRepository(ISqlConnectionFactory connectionFactory) : IEmployeeRepository
{
    private const string Columns =
        "Id, Name, Email, Department, ProfilePictureUrl, CreatedAtUtc, UpdatedAtUtc";

    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            $"SELECT {Columns} FROM dbo.Employees ORDER BY Name",
            cancellationToken: cancellationToken);

        var employees = await connection.QueryAsync<Employee>(command);
        return employees.AsList();
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            $"SELECT {Columns} FROM dbo.Employees WHERE Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Employee>(command);
    }

    public async Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Employees
                (Id, Name, Email, Department, ProfilePictureUrl, CreatedAtUtc, UpdatedAtUtc)
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Email, INSERTED.Department,
                   INSERTED.ProfilePictureUrl, INSERTED.CreatedAtUtc, INSERTED.UpdatedAtUtc
            VALUES
                (@Id, @Name, @Email, @Department, @ProfilePictureUrl, @CreatedAtUtc, @UpdatedAtUtc);
            """;

        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, employee, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<Employee>(command);
    }

    public async Task<Employee?> UpdateAsync(Employee employee, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Employees
            SET Name = @Name,
                Email = @Email,
                Department = @Department,
                ProfilePictureUrl = @ProfilePictureUrl,
                UpdatedAtUtc = @UpdatedAtUtc
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Email, INSERTED.Department,
                   INSERTED.ProfilePictureUrl, INSERTED.CreatedAtUtc, INSERTED.UpdatedAtUtc
            WHERE Id = @Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, employee, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Employee>(command);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM dbo.Employees WHERE Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command) > 0;
    }
}
