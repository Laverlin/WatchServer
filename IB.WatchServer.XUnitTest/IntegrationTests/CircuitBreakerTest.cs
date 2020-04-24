using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Service.HttpClients;
using IB.WatchServer.XUnitTest.UnitTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace IB.WatchServer.XUnitTest.IntegrationTests
{
    public class CircuitBreakerTest
    {
                [Fact]
        public async void After2FaultsCircuitBreakerShouldSendRequestsToFallback()
        {
            // Arrange
            //
            var config = new ConfigurationBuilder()
                //.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.Development.json", false, true)
                .Build();
            var settings = config.LoadVerifiedConfiguration<FaceSettings>();
            
            var mainUrl1 = settings.BuildCurrencyConverterUrl("USD", "RUB");
            var mainUrl2 = settings.BuildCurrencyConverterUrl("EUR", "PHP");
            var fallbackUrl1 = settings.BuildExchangeRateApiUrl("USD", "RUB");
            var fallbackUrl2 = settings.BuildExchangeRateApiUrl("EUR", "PHP");


            var handler = new Mock<HttpMessageHandler>();
            handler.SetupRequest(HttpMethod.Get, mainUrl1)
                .ReturnsResponse(HttpStatusCode.ServiceUnavailable)
                .Verifiable();
            handler.SetupRequest(HttpMethod.Get, mainUrl2)
                .ReturnsResponse(HttpStatusCode.ServiceUnavailable)
                .Verifiable();
            handler.SetupRequest(HttpMethod.Get, fallbackUrl1)
                .ReturnsResponse(HttpStatusCode.OK,"{\"rates\":{\"RUB\":50.9298531811},\"base\":\"USD\",\"date\":\"2020-03-30\"}")
                .Verifiable();
            handler.SetupRequest(HttpMethod.Get, fallbackUrl2)
                .ReturnsResponse(HttpStatusCode.OK,"{\"rates\":{\"PHP\":50.9298531811},\"base\":\"EUR\",\"date\":\"2020-03-30\"}")
                .Verifiable();

            var measureCounterMetrics = new Mock<IMeasureCounterMetrics>();
            
            var measureMetricMock = new Mock<IMeasureMetrics>();
            measureMetricMock.Setup(_ => _.Counter).Returns(measureCounterMetrics.Object);
            var metricsMock = new Mock<IMetrics>();
            metricsMock.Setup(_ => _.Measure).Returns(measureMetricMock.Object);

            var client = handler.CreateClient();

            IServiceCollection services = new ServiceCollection();
            


            var loggerCccMock = new Mock<ILogger<CurrencyConverterClient>>();
            var loggerErcMock = new Mock<ILogger<ExchangeRateApiClient>>();
            
           // var ccc = new CurrencyConverterClient(loggerCccMock.Object, handler.CreateClient(), settings, metricsMock.Object);
           // var erc = new ExchangeRateApiClient(loggerErcMock.Object, handler.CreateClient(), settings, metricsMock.Object);

            services.AddSingleton(settings);
            services.AddSingleton(loggerCccMock.Object);
            services.AddSingleton(loggerErcMock.Object);
            services.AddSingleton(metricsMock.Object);

            services.AddHttpClient<CurrencyConverterClient>()
                .AddRetryPolicyWithCb(2, TimeSpan.FromMinutes(10))
                .AddHttpMessageHandler(()=>new StubDelegatingHandler(client));
            services.AddHttpClient<ExchangeRateApiClient>()
                .AddRetryPolicyWithCb(2, TimeSpan.FromMinutes(10))
                .AddHttpMessageHandler(()=>new StubDelegatingHandler(client));



            var isp = services.BuildServiceProvider();

            var webRequestProvider = new ExchangeRateCacheStrategy(
                TestHelper.GetLoggerMock<ExchangeRateCacheStrategy>().Object,
                isp.GetRequiredService<CurrencyConverterClient>(), isp.GetRequiredService<ExchangeRateApiClient>(),
                settings, metricsMock.Object);


            // Act
            //
            var result1 = await webRequestProvider.GetExchangeRate("USD", "RUB");
            var result2 = await webRequestProvider.GetExchangeRate("EUR", "PHP");

            // Assert
            //
            handler.VerifyRequest(HttpMethod.Get, mainUrl1, Times.Exactly(2));
            handler.VerifyRequest(HttpMethod.Get, mainUrl2, Times.Exactly(0));
            handler.VerifyRequest(HttpMethod.Get, fallbackUrl1, Times.Exactly(1));
            handler.VerifyRequest(HttpMethod.Get, fallbackUrl2, Times.Exactly(1));
           
        } 

    }

    public class StubDelegatingHandler : DelegatingHandler
    {
        //private readonly HttpStatusCode stubHttpStatusCode;
        private readonly HttpClient _httpClient;
        public StubDelegatingHandler(HttpClient httpClient) => _httpClient = httpClient;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => 
            _httpClient.SendAsync(Clone(request), cancellationToken);

        public HttpRequestMessage Clone(HttpRequestMessage req)
        {
            HttpRequestMessage clone = new HttpRequestMessage(req.Method, req.RequestUri);

            clone.Content = req.Content;
            clone.Version = req.Version;

            foreach (KeyValuePair<string, object> prop in req.Properties)
            {
                clone.Properties.Add(prop);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
