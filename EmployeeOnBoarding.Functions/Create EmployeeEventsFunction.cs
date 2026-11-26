using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EmployeeOnBoarding.Contracts;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmployeeOnBoarding.Functions;

public sealed class EmployeeEventsFunction(
    INotificationRepository repository,
    ILogger<EmployeeEventsFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(EmployeeEventsFunction))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var employeeChangedEvent = JsonSerializer.Deserialize<EmployeeChangedEvent>(
            message.Body,
            JsonOptions);

        if (employeeChangedEvent is null)
        {
            throw new InvalidOperationException(
                $"Service Bus message {message.MessageId} could not be deserialized.");
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
