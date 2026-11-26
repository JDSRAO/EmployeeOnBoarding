using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using EmployeeOnBoarding.Api.Infrastructure;
using EmployeeOnBoarding.Api.Models;
using EmployeeOnBoarding.Api.Repositories;
using EmployeeOnBoarding.Api.Services;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext(), writeToProviders: true);

if (!string.IsNullOrWhiteSpace(
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(ServiceBusOptions.SectionName));

var sqlConnectionString = builder.Configuration.GetConnectionString("SqlDatabase");
if (string.IsNullOrWhiteSpace(sqlConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:SqlDatabase is required. Configure it with user secrets or App Service settings.");
}

var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage");
if (string.IsNullOrWhiteSpace(blobConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:BlobStorage is required. Configure it with user secrets or App Service settings.");
}

var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    throw new InvalidOperationException(
        "ServiceBus:ConnectionString is required. Configure it with user secrets or App Service settings.");
}

builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(sqlConnectionString));
builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return serviceProvider.GetRequiredService<ServiceBusClient>().CreateSender(options.QueueName);
});

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
