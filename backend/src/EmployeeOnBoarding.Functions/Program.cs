using EmployeeOnBoarding.Functions.Infrastructure;
using EmployeeOnBoarding.Functions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // local.settings.json Values use ConnectionStrings__SqlDatabase (env-var form).
        // Azure Function App settings / connection strings use the same key.
        var sqlConnectionString = context.Configuration.GetConnectionString("SqlDatabase");

        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:SqlDatabase is required. " +
                "Set ConnectionStrings__SqlDatabase in local.settings.json (local) or Function App settings (Azure).");
        }

        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(sqlConnectionString));
        services.AddScoped<INotificationRepository, NotificationRepository>();
    })
    .Build();

host.Run();
