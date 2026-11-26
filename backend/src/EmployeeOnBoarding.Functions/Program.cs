using EmployeeOnBoarding.Functions.Infrastructure;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var sqlConnectionString =
    builder.Configuration.GetConnectionString("SqlDatabase");

if (string.IsNullOrWhiteSpace(sqlConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:SqlDatabase is required.");
}

builder.Services.AddSingleton<ISqlConnectionFactory>(
    new SqlConnectionFactory(sqlConnectionString));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Build().Run();
