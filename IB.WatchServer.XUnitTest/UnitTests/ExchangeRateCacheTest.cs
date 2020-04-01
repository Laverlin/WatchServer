using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Service.HttpClients;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net.Http;
using System.Threading.Tasks;
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
            currencyConverterClientMock
                .Setup(_ => _.RequestCurrencyConverter("USD", "EUR"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = (decimal) 1.1, RequestStatus = new RequestStatus(RequestStatusCode.Ok)}))
                .Verifiable();

            var webRequestProvider = new ExchangeRateCacheStrategy(
                currencyConverterClientMock.Object, exchangeApiClientMock.Object, 
                TestHelper.GetFaceSettings(), TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await webRequestProvider.GetExchangeRate("USD", "EUR");


            // Assert
            //
            currencyConverterClientMock.Verify(_ => _.RequestCurrencyConverter("USD", "EUR"), Times.Once);
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
                currencyConverterClientMock.Object, null, 
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
                    {ExchangeRate = 0, RequestStatus = new RequestStatus(RequestStatusCode.Error)}))
                .Verifiable();
            var exchangeApiClientMock = new Mock<ExchangeRateApiClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            exchangeApiClientMock
                .Setup(_ => _.RequestExchangeRateApi("CHF", "EUR"))
                .Returns(() => Task.FromResult(new ExchangeRateInfo
                    {ExchangeRate = (decimal) 1.1, RequestStatus = new RequestStatus(RequestStatusCode.Ok)}))
                .Verifiable();

            var webRequestProvider = new ExchangeRateCacheStrategy(
                currencyConverterClientMock.Object, exchangeApiClientMock.Object, 
                TestHelper.GetFaceSettings(), TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await webRequestProvider.GetExchangeRate("CHF", "EUR");


            // Assert
            //
            currencyConverterClientMock.Verify(_ => _.RequestCurrencyConverter("CHF", "EUR"), Times.Once);
            exchangeApiClientMock.Verify(_ => _.RequestExchangeRateApi("CHF", "EUR"), Times.Once);
            Assert.Equal((decimal)1.1, result.ExchangeRate);
        }

        [Fact]
        public async void ExchangeRateWithErrorShouldFallbackAndReturnZeroForUnsupportedPair()
        {
            // Arrange
            //
            var handler = new Mock<HttpMessageHandler>();
            var currencyConverterClientMock = new Mock<CurrencyConverterClient>(
                MockBehavior.Loose, null, handler.CreateClient(), null,null);
            currencyConverterClientMock
                .Setup(_ => _.RequestCurrencyConverter("CHF", "BTC"))
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

            var webRequestProvider = new ExchangeRateCacheStrategy(
                currencyConverterClientMock.Object, exchangeApiClientMock.Object, 
                TestHelper.GetFaceSettings(), TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await webRequestProvider.GetExchangeRate("CHF", "BTC");


            // Assert
            //
            currencyConverterClientMock.Verify(_ => _.RequestCurrencyConverter(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            exchangeApiClientMock.Verify(_ => _.RequestExchangeRateApi(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.Equal(0, result.ExchangeRate);
            Assert.Equal(RequestStatusCode.HasNotBeenRequested, result.RequestStatus.StatusCode);
        }

    }

}
