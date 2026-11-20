using Dapper;
using EmployeeOnBoarding.Api.Infrastructure;
using EmployeeOnBoarding.Api.Models;

namespace EmployeeOnBoarding.Api.Repositories;

public sealed class NotificationRepository(ISqlConnectionFactory connectionFactory) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (@Take)
                Id, EventId, EmployeeId, EmployeeName, EventType, Message, CreatedAtUtc
            FROM dbo.Notifications
            ORDER BY CreatedAtUtc DESC;
            """;

        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { Take = take }, cancellationToken: cancellationToken);
        var notifications = await connection.QueryAsync<Notification>(command);
        return notifications.AsList();
    }
}
