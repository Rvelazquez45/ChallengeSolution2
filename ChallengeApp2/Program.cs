using ChallengeApp2;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration
    (
        configuration =>
        {// The order matters. There's an override in a downward direction if there are duplicate keys.
            configuration.AddJsonFile(path: "appsettings.json", optional: true);
            configuration.AddJsonFile(path: "local.settings.json", optional: true);
            configuration.AddEnvironmentVariables();
            configuration.AddUserSecrets<CreateCSV>();
        }
    )
    .ConfigureServices
    (
        services =>
            {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
        }
    ).Build();

host.Run();
