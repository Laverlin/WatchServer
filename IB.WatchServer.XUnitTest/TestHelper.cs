using System;
using System.Collections.Generic;
using System.Text;
using App.Metrics;
using App.Metrics.Counter;
using IB.WatchServer.Service;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace IB.WatchServer.XUnitTest
{
    public class TestHelper
    {
        public static Mock<ILogger<T>> GetLoggerMock<T>()
        {
            return new Mock<ILogger<T>>();
        }

        public static FaceSettings GetFaceSettings()
        {
            var config = new ConfigurationBuilder()
                //.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.Development.json", false, true)
                .Build();
            var settings = config.LoadVerifiedConfiguration<FaceSettings>();

            return settings;
        }

        public static IConnectionSettings GetConnectionSettings()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddUserSecrets<Startup>()
                .AddJsonFile("appsettings.Test.json", false, true)
                .Build();
            var settings = config.LoadVerifiedConfiguration<PostgresProviderSettings>();

            return settings;
        }

        public static Mock<IMetrics> GetMetricsMock()
        {
            var measureCounterMetrics = new Mock<IMeasureCounterMetrics>();
            
            var measureMetricMock = new Mock<IMeasureMetrics>();
            measureMetricMock.Setup(_ => _.Counter).Returns(measureCounterMetrics.Object);

            var metricsMock = new Mock<IMetrics>();
            metricsMock.Setup(_ => _.Measure).Returns(measureMetricMock.Object);

            return metricsMock;
        }
    }
}
