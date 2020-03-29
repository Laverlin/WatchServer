using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using App.Metrics;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Service;
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
 //           var handler = new Mock<HttpMessageHandler>();
 //           handler.SetupAnyRequest()
 //               .ReturnsResponse(HttpStatusCode.OK, "{\"USD_PHP\": {\r\n\"val\": 51.440375\r\n}}")
 //               .Verifiable();

            var metricsMock = new Mock<IMetrics>();

 //           var httpClientFactoryMock = handler.CreateClientFactory();
            var webRequestProvider = new WebRequestsProvider(null, null, null, metricsMock.Object, null);

            string actualBase="";
            string actualTarget="";
            int callCount = 0;

            // Act
            //
            await webRequestProvider.RequestCacheExchangeRate("USD", "PHP", 
                (b, t) =>
                {
                    actualBase = b;
                    actualTarget = t;
                    callCount++;
                    return Task.FromResult(new ExchangeRateInfo {ExchangeRate = (decimal) 51.440375});
                });

            // Assert
            //
            //handler.VerifyAnyRequest(Times.Exactly(1));
            Assert.Equal("USD", actualBase);
            Assert.Equal("PHP", actualTarget);
            Assert.Equal(1, callCount);
        }
    }
}
