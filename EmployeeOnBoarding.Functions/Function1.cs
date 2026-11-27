using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EmployeeOnBoarding.Contracts;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmployeeOnBoarding.Functions;
public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly INotificationRepository _repository;
    //3This is the Azure Function that replaces the old EmployeeEventsWorker.

    public Function1(
        ILogger<Function1> logger,
        INotificationRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    //5This automatically triggers the function whenever a message arrives in the employee-events queue.
    [Function("EmployeeEventsFunction")]
    public async Task Run(
        [ServiceBusTrigger("employee-events", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken)
    {
        //This converts the Service Bus message into an EmployeeChangedEvent object.
        var employeeChangedEvent =
            JsonSerializer.Deserialize<EmployeeChangedEvent>(
                message.Body.ToString());

        if (employeeChangedEvent is null)
        {
            throw new InvalidOperationException(
                $"Message {message.MessageId} could not be deserialized.");
        }

        var created = await _repository.CreateIfNotExistsAsync(
            employeeChangedEvent,
            cancellationToken);

        if (created)
        {
            _logger.LogInformation(
                "Notification created for Employee {EmployeeId}",
                employeeChangedEvent.EmployeeId);
        }
        //msg sucful
        await messageActions.CompleteMessageAsync(message);
    }
}
