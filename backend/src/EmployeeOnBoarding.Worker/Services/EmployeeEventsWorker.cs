using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EmployeeOnBoarding.Contracts;
using EmployeeOnBoarding.Worker.Repositories;

namespace EmployeeOnBoarding.Worker.Services;

public sealed class EmployeeEventsWorker(
    ServiceBusClient serviceBusClient,
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<EmployeeEventsWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = configuration["ServiceBus:QueueName"]
            ?? ServiceBusDefaults.EmployeeEventsQueue;

        await using var processor = serviceBusClient.CreateProcessor(
            queueName,
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 2,
                AutoCompleteMessages = true,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            });

        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Worker listening on Service Bus queue {QueueName}", queueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Worker shutdown requested");
        }

        await processor.StopProcessingAsync(CancellationToken.None);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var employeeChangedEvent = JsonSerializer.Deserialize<EmployeeChangedEvent>(
            args.Message.Body,
            JsonOptions);

        if (employeeChangedEvent is null)
        {
            throw new InvalidOperationException(
                $"Service Bus message {args.Message.MessageId} could not be deserialized.");
        }

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var created = await repository.CreateIfNotExistsAsync(
            employeeChangedEvent,
            args.CancellationToken);

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

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Service Bus error. Source: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }
}
