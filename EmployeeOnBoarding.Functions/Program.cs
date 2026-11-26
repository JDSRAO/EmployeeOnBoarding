using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using EmployeeOnBoarding.Functions.Infrastructure;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

var sqlConnectionString = builder.Configuration.GetConnectionString("SqlDatabase");
if (string.IsNullOrWhiteSpace(sqlConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:SqlDatabase is required. Configure it with local settings or Function App settings.");
}

builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(sqlConnectionString));
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Build().Run();
