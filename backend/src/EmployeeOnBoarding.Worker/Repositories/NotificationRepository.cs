using Dapper;
using EmployeeOnBoarding.Contracts;
using EmployeeOnBoarding.Worker.Infrastructure;

namespace EmployeeOnBoarding.Worker.Repositories;

public sealed class NotificationRepository(ISqlConnectionFactory connectionFactory) : INotificationRepository
{
    public async Task<bool> CreateIfNotExistsAsync(
        EmployeeChangedEvent employeeChangedEvent,
        CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Notifications WHERE EventId = @EventId)
            BEGIN
                INSERT INTO dbo.Notifications
                    (EventId, EmployeeId, EmployeeName, EventType, Message, CreatedAtUtc)
                VALUES
                    (@EventId, @EmployeeId, @EmployeeName, @EventType, @Message, SYSUTCDATETIME());

                SELECT CAST(1 AS bit);
            END
            ELSE
            BEGIN
                SELECT CAST(0 AS bit);
            END
            """;

        var message = employeeChangedEvent.EventType == EmployeeEventType.Created.ToString()
            ? $"{employeeChangedEvent.Name} was added to {employeeChangedEvent.Department}."
            : $"{employeeChangedEvent.Name}'s profile was updated in {employeeChangedEvent.Department}.";

        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                employeeChangedEvent.EventId,
                employeeChangedEvent.EmployeeId,
                EmployeeName = employeeChangedEvent.Name,
                employeeChangedEvent.EventType,
                Message = message
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<bool>(command);
    }
}
