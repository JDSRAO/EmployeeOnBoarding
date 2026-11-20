using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EmployeeOnBoarding.Contracts;

namespace EmployeeOnBoarding.Api.Services;

public interface IEventPublisher
{
    Task PublishEmployeeChangedAsync(EmployeeChangedEvent employeeChangedEvent, CancellationToken cancellationToken);
}

public sealed class ServiceBusEventPublisher(
    ServiceBusSender sender,
    ILogger<ServiceBusEventPublisher> logger) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishEmployeeChangedAsync(
        EmployeeChangedEvent employeeChangedEvent,
        CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(employeeChangedEvent, JsonOptions))
        {
            MessageId = employeeChangedEvent.EventId.ToString(),
            Subject = employeeChangedEvent.EventType,
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);

        logger.LogInformation(
            "Published {EventType} event {EventId} for employee {EmployeeId}",
            employeeChangedEvent.EventType,
            employeeChangedEvent.EventId,
            employeeChangedEvent.EmployeeId);
    }
}
