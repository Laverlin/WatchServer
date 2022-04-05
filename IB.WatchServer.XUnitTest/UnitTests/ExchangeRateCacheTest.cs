using System;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Service.HttpClients;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class ExchangeRateCacheTest
    {
        [Fact]
        public async void ExchangeRateWithNewCurrencyPairShouldMakeRequest()
        {
            // Arrange
            //
            var handler = new Mock<HttpMessageHandler>();
            var currencyConverterClientMock = new Mock<CurrencyConverterClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            var exchangeApiClientMock = new Mock<ExchangeRateApiClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);

            var rateHostClientMock = new Mock<ExchangeRateHostClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            rateHostClientMock
                .Setup(_ => _.RequestExchangeRateHostApi("USD", "EUR"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = (decimal) 1.1, RequestStatus = new RequestStatus(RequestStatusCode.Ok)}))
                .Verifiable();

            var webRequestProvider = new ExchangeRateCacheStrategy(
                TestHelper.GetLoggerMock<ExchangeRateCacheStrategy>().Object,
                currencyConverterClientMock.Object, exchangeApiClientMock.Object, 
                rateHostClientMock.Object,
                TestHelper.GetFaceSettings(), TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await webRequestProvider.GetExchangeRate("USD", "EUR");


            // Assert
            //
            rateHostClientMock.Verify(_ => _.RequestExchangeRateHostApi("USD", "EUR"), Times.Once);
            Assert.Equal((decimal)1.1, result.ExchangeRate);
        }


        [Fact]
        public async void SecondRequestWithSameCurrencyShouldBeFromCache()
        {
            // Arrange
            //
            var handler = new Mock<HttpMessageHandler>();
            var currencyConverterClientMock = new Mock<CurrencyConverterClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);

            currencyConverterClientMock
                .Setup(_ => _.RequestCurrencyConverter("USD", "CHF"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                {
                    ExchangeRate = (decimal) 1.1,
                    RequestStatus = new RequestStatus(RequestStatusCode.Ok)
                }))
                .Verifiable();

            var webRequestProvider = new ExchangeRateCacheStrategy(
                TestHelper.GetLoggerMock<ExchangeRateCacheStrategy>().Object,
                currencyConverterClientMock.Object, null, null,
                TestHelper.GetFaceSettings(), TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result1= await webRequestProvider.GetExchangeRate("USD", "CHF");
            var result2 = await webRequestProvider.GetExchangeRate("USD", "CHF");

            // Assert
            //
            currencyConverterClientMock.Verify(_ => _.RequestCurrencyConverter("USD", "CHF"), Times.Once);
            Assert.Equal((decimal)1.1, result1.ExchangeRate);
            Assert.Equal((decimal)1.1, result2.ExchangeRate);
        }

        [Fact]
        public async void ExchangeRateWithErrorShouldFallbackToAnotherProvider()
        {
            // Arrange
            //
            var handler = new Mock<HttpMessageHandler>();
            var currencyConverterClientMock = new Mock<CurrencyConverterClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            currencyConverterClientMock
                .Setup(_ => _.RequestCurrencyConverter("CHF", "EUR"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = (decimal) 1.1, RequestStatus = new RequestStatus(RequestStatusCode.Ok)}))
                .Verifiable();
            var exchangeApiClientMock = new Mock<ExchangeRateApiClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            exchangeApiClientMock
                .Setup(_ => _.RequestExchangeRateApi("CHF", "EUR"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = (decimal) 1.1, RequestStatus = new RequestStatus(RequestStatusCode.Ok)}))
                .Verifiable();

            var rateHostClientMock = new Mock<ExchangeRateHostClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            rateHostClientMock
                .Setup(_ => _.RequestExchangeRateHostApi("CHF", "EUR"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = 0, RequestStatus = new RequestStatus(RequestStatusCode.Error)}))
                .Verifiable();

            var webRequestProvider = new ExchangeRateCacheStrategy(
                TestHelper.GetLoggerMock<ExchangeRateCacheStrategy>().Object,
                currencyConverterClientMock.Object, exchangeApiClientMock.Object, 
                rateHostClientMock.Object,
                TestHelper.GetFaceSettings(), TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await webRequestProvider.GetExchangeRate("CHF", "EUR");


            // Assert
            //
            currencyConverterClientMock.Verify(_ => _.RequestCurrencyConverter("CHF", "EUR"), Times.Once);
            rateHostClientMock.Verify(_ => _.RequestExchangeRateHostApi("CHF", "EUR"), Times.Once);
            Assert.Equal((decimal)1.1, result.ExchangeRate);
        }


        [Fact]
        public async void ExchangeRateWithErrorShouldFallbackAndProvideLogRecord()
        {
            // Arrange
            //
            var handler = new Mock<HttpMessageHandler>();
            var currencyConverterClientMock = new Mock<CurrencyConverterClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            currencyConverterClientMock
                .Setup(_ => _.RequestCurrencyConverter("CHF", "BTC"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = (decimal)1.1, RequestStatus = new RequestStatus(RequestStatusCode.Ok)}))
                .Verifiable();

            var rateHostClientMock = new Mock<ExchangeRateHostClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            rateHostClientMock
                .Setup(_ => _.RequestExchangeRateHostApi("CHF", "BTC"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = 0, RequestStatus = new RequestStatus(RequestStatusCode.Error)}))
                .Verifiable();

            var exchangeApiClientMock = new Mock<ExchangeRateApiClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            exchangeApiClientMock
                .Setup(_ => _.RequestExchangeRateApi("CHF", "BTC"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = (decimal) 1.1, RequestStatus = new RequestStatus(RequestStatusCode.Ok)}))
                .Verifiable();

            var loggerMock = TestHelper.GetLoggerMock<ExchangeRateCacheStrategy>();

            var webRequestProvider = new ExchangeRateCacheStrategy(
                loggerMock .Object,
                currencyConverterClientMock.Object, exchangeApiClientMock.Object, rateHostClientMock.Object,
                TestHelper.GetFaceSettings(), TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await webRequestProvider.GetExchangeRate("CHF", "BTC");


            // Assert
            //
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().StartsWith("Fallback, object state ")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>) It.IsAny<object>()),
                Times.Once);
        }

    }

}
