using Azure.Messaging.ServiceBus;
using EmployeeOnBoarding.Worker.Infrastructure;
using EmployeeOnBoarding.Worker.Repositories;
using EmployeeOnBoarding.Worker.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext(), writeToProviders: true);

builder.Services.AddHealthChecks();

var sqlConnectionString = builder.Configuration.GetConnectionString("SqlDatabase");
if (string.IsNullOrWhiteSpace(sqlConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:SqlDatabase is required. Configure it with user secrets or App Service settings.");
}

var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    throw new InvalidOperationException(
        "ServiceBus:ConnectionString is required. Configure it with user secrets or App Service settings.");
}

builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(sqlConnectionString));
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddHostedService<EmployeeEventsWorker>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");

app.Run();
