using IB.WatchServer.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using IB.WatchServer.RequestCollector;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.Azure.WebJobs;

[assembly: FunctionsStartup(typeof(StartupFunction))]
namespace IB.WatchServer.RequestCollector
{
    /// <summary>
    /// Startup function to configure the Azure Function
    /// </summary>
    public class StartupFunction : IWebJobsStartup
    {
        /// <summary>
        /// Load configuration from user secrets and environment variables
        /// </summary>

        public void Configure(IWebJobsBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<StartupFunction>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Registering Serilog provider
            //
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            builder.Services.AddLogging(lb => lb.AddSerilog(logger, true));
            Log.Logger = logger;

            var collectorSettings = configuration.LoadVerifiedConfiguration<KafkaSettings>();

            builder.Services.AddSingleton(collectorSettings);
        }
    }
}
