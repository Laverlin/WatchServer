using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class WebRequestProviderTest
    {
        [Fact]
        public async void ExchangeRateWithNewCurrencyPairShouldMakeRequest()
        {
            // Arrange
            //
            var config = new ConfigurationBuilder()
                //.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.Development.json", false, true)
                .Build();
            var settings = config.LoadVerifiedConfiguration<FaceSettings>();

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK, "{\"USD_EUR\": 51.440375}")
                .Verifiable();

            var measureCounterMetrics = new Mock<IMeasureCounterMetrics>();
            
            var measureMetricMock = new Mock<IMeasureMetrics>();
            measureMetricMock.Setup(_ => _.Counter).Returns(measureCounterMetrics.Object);
            var metricsMock = new Mock<IMetrics>();
            metricsMock.Setup(_ => _.Measure).Returns(measureMetricMock.Object);

            var httpClientFactoryMock = handler.CreateClientFactory();



            var webRequestProvider = new WebRequestsProvider(null, httpClientFactoryMock, settings, metricsMock.Object, null);

            // Act
            //
            await webRequestProvider.RequestCacheExchangeRate("USD", "EUR");

            // Assert
            //
            handler.VerifyAnyRequest(Times.Exactly(1));
           
        }


        [Fact]
        public async void SecondRequestWithSameCurrancyShouldbeFromCache()
        {
            // Arrange
            //
            var config = new ConfigurationBuilder()
                //.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.Development.json", false, true)
                .Build();
            var settings = config.LoadVerifiedConfiguration<FaceSettings>();

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK, "{\"USD_CHF\": 51.440375}")
                .Verifiable();

            var measureCounterMetrics = new Mock<IMeasureCounterMetrics>();
            
            var measureMetricMock = new Mock<IMeasureMetrics>();
            measureMetricMock.Setup(_ => _.Counter).Returns(measureCounterMetrics.Object);
            var metricsMock = new Mock<IMetrics>();
            metricsMock.Setup(_ => _.Measure).Returns(measureMetricMock.Object);

            var httpClientFactoryMock = handler.CreateClientFactory();



            var webRequestProvider = new WebRequestsProvider(null, httpClientFactoryMock, settings, metricsMock.Object, null);

            // Act
            //
            await webRequestProvider.RequestCacheExchangeRate("USD", "CHF");
            await webRequestProvider.RequestCacheExchangeRate("USD", "CHF");

            // Assert
            //
            handler.VerifyAnyRequest(Times.Exactly(1));
           
        }

        [Fact]
        public async void ExchangeRateWithErrorShouldFallbackToAnotherProvider()
        {
            // Arrange
            //
            var config = new ConfigurationBuilder()
                //.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.Development.json", false, true)
                .Build();
            var settings = config.LoadVerifiedConfiguration<FaceSettings>();
            
            var mainUrl = settings.BuildCurrencyConverterUrl("USD", "PHP");
            var fallbackUrl = settings.BuildExchangeRateApiUrl("USD", "PHP");


            var handler = new Mock<HttpMessageHandler>();
            handler.SetupRequest(HttpMethod.Get, mainUrl)
                .ReturnsResponse(HttpStatusCode.ServiceUnavailable)
                .Verifiable();
            handler.SetupRequest(HttpMethod.Get, fallbackUrl)
                .ReturnsResponse(HttpStatusCode.OK,"{\"rates\":{\"PHP\":50.9298531811},\"base\":\"USD\",\"date\":\"2020-03-30\"}")
                .Verifiable();

            var measureCounterMetrics = new Mock<IMeasureCounterMetrics>();
            
            var measureMetricMock = new Mock<IMeasureMetrics>();
            measureMetricMock.Setup(_ => _.Counter).Returns(measureCounterMetrics.Object);
            var metricsMock = new Mock<IMetrics>();
            metricsMock.Setup(_ => _.Measure).Returns(measureMetricMock.Object);

            var httpClientFactoryMock = handler.CreateClientFactory();


            var webRequestProvider = new WebRequestsProvider(null, httpClientFactoryMock, settings, metricsMock.Object, null);


            // Act
            //
            await webRequestProvider.RequestCacheExchangeRate("USD", "PHP");

            // Assert
            //
            handler.VerifyRequest(HttpMethod.Get, mainUrl, Times.Exactly(1));
            handler.VerifyRequest(HttpMethod.Get, fallbackUrl, Times.Exactly(1));
           
        }

    }
}
