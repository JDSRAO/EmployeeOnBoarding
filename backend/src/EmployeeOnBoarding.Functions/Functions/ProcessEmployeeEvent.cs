using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EmployeeOnBoarding.Contracts;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmployeeOnBoarding.Functions.Functions;

/// <summary>
/// Azure Functions replacement for EmployeeOnBoarding.Worker.EmployeeEventsWorker.
/// Trigger: Service Bus queue (default employee-events). Same deserialize + idempotent SQL insert.
/// </summary>
public sealed class ProcessEmployeeEvent(
    INotificationRepository notificationRepository,
    ILogger<ProcessEmployeeEvent> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(ProcessEmployeeEvent))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        // Same contract + JSON options as EmployeeOnBoarding.Worker.EmployeeEventsWorker
        var employeeChangedEvent = JsonSerializer.Deserialize<EmployeeChangedEvent>(
            message.Body,
            JsonOptions);

        if (employeeChangedEvent is null)
        {
            // Throwing lets the Functions host retry, then dead-letter after max delivery count.
            throw new InvalidOperationException(
                $"Service Bus message {message.MessageId} could not be deserialized.");
        }

        var created = await notificationRepository.CreateIfNotExistsAsync(
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
