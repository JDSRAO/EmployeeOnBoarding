using Azure.Monitor.OpenTelemetry.Exporter;
using EmployeeOnBoarding.Functions.Infrastructure;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
//7configures and starts the Azure Functions runtime.
//9registers SQL connection creation for database access.
//2injects the repository that saves notifications to SQL.

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

var sqlConnectionString =
    builder.Configuration.GetConnectionString("SqlDatabase");

builder.Services.AddSingleton<ISqlConnectionFactory>(
    new SqlConnectionFactory(sqlConnectionString!));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Build().Run();
