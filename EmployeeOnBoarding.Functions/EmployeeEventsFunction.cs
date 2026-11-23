using System.Text.Json;
using EmployeeOnBoarding.Contracts;
using EmployeeOnBoarding.Worker.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;

namespace EmployeeOnBoarding.Functions;

public class EmployeeEventsFunction
{
    private readonly INotificationRepository repository;
    private readonly ILogger<EmployeeEventsFunction> logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public EmployeeEventsFunction(
        INotificationRepository repository,
        ILogger<EmployeeEventsFunction> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    [Function("EmployeeEventsFunction")]
    public async Task Run(
        [ServiceBusTrigger(
            "%ServiceBus:QueueName%",
            Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var employeeChangedEvent =
            JsonSerializer.Deserialize<EmployeeChangedEvent>(
                message,
                JsonOptions);

        if (employeeChangedEvent is null)
        {
            throw new InvalidOperationException(
                "Message could not be deserialized.");
        }

        var created =
            await repository.CreateIfNotExistsAsync(
                employeeChangedEvent,
                cancellationToken);

        if (created)
        {
            logger.LogInformation(
                "Notification created for EmployeeId {EmployeeId}",
                employeeChangedEvent.EmployeeId);
        }
        else
        {
            logger.LogWarning(
                "Duplicate event {EventId}",
                employeeChangedEvent.EventId);
        }
    }
}
