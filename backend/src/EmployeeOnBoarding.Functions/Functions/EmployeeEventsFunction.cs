using System.Text.Json;
using EmployeeOnBoarding.Contracts;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmployeeOnBoarding.Functions.Functions;

public sealed class EmployeeEventsFunction(
    INotificationRepository repository,
    ILogger<EmployeeEventsFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Function("EmployeeEventsFunction")]
    public async Task Run(
        [ServiceBusTrigger("%EmployeeEventsQueue%", Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var employeeChangedEvent =
            JsonSerializer.Deserialize<EmployeeChangedEvent>(message, JsonOptions);

        if (employeeChangedEvent is null)
        {
            throw new InvalidOperationException(
                "Service Bus message could not be deserialized.");
        }

        var created = await repository.CreateIfNotExistsAsync(
            employeeChangedEvent,
            cancellationToken);

        if (created)
        {
            logger.LogInformation(
                "Processed {EventType} event {EventId} and created notification for employee {EmployeeId}",
                employeeChangedEvent.EventType,
                employeeChangedEvent.EventId,
                employeeChangedEvent.EmployeeId);
        }
        else
        {
            logger.LogWarning(
                "Skipped duplicate event {EventId}; notification already exists",
                employeeChangedEvent.EventId);
        }
    }
}
