using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using IB.WatchServer.RequestCollector;

[assembly: FunctionsStartup(typeof(StartupFunction))]
namespace IB.WatchServer.RequestCollector
{
    /// <summary>
    /// Startup function to configure the Azure Function
    /// </summary>
    public class StartupFunction : FunctionsStartup
    {
        /// <summary>
        /// Load configuration from user secrets and environment variables
        /// </summary>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<StartupFunction>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var collectorSettings = configuration.GetSection(typeof(CollectorSettings).Name).Get<CollectorSettings>();

            builder.Services.AddSingleton(collectorSettings);
        }
    }
}
