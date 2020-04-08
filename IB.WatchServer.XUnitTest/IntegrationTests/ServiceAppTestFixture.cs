using App.Metrics;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Migrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace IB.WatchServer.XUnitTest.IntegrationTests
{
    public class ServiceAppTestFixture : WebApplicationFactory<Service.Program>
    {

        public ITestOutputHelper Output { get; set; }

        // Uses the generic host
        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = base.CreateHostBuilder();
            builder.ConfigureAppConfiguration(config => config.AddJsonFile("appsettings.Test.json", false, true));

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders(); // Remove other loggers
                logging.AddXUnit(Output); // Use the ITestOutputHelper instance
            });

            return builder;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices((services) =>
            {
                services.RemoveAll<IMetricsRoot>();
                services.RemoveAll<IHostedService>();
            });

            
        }
    }
}