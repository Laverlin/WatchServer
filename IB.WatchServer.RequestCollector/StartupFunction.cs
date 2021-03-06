﻿using AutoMapper;
using IB.WatchServer.Abstract;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Abstract.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using IB.WatchServer.RequestCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs.Hosting;
using Serilog;
using Microsoft.Azure.WebJobs;

[assembly: FunctionsStartup(typeof(IB.WatchServer.RequestCollector.StartupFunction))]
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
               // .AddUserSecrets<StartupFunction>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Registering Serilog provider
            //
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
                .CreateLogger();
            builder.Services.AddLogging(logBuilder => 
                logBuilder.AddSerilog(logger, true));
            Log.Logger = logger;

            

            var collectorSettings = configuration.LoadVerifiedConfiguration<KafkaSettings>();
            var msSqlConnectionSettings = configuration.LoadVerifiedConfiguration<MsSqlProviderSettings>();

            builder.Services.AddSingleton(collectorSettings);
            builder.Services.AddSingleton(msSqlConnectionSettings);
            builder.Services.AddSingleton(new DataConnectionFactory(msSqlConnectionSettings));
            builder.Services.AddSingleton(new MapperConfiguration(mc =>
            {
                mc.CreateMap<WatchRequest, RequestData>();
                mc.CreateMap<WeatherInfo, RequestData>();
                mc.CreateMap<LocationInfo, RequestData>();
                mc.CreateMap<ExchangeRateInfo, RequestData>();
            }).CreateMapper());
        }
    }
}
