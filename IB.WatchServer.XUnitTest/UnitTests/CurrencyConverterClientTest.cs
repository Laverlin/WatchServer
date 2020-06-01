using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Service.Service.HttpClients;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class CurrencyConverterClientTest
    {
        [Fact]
        public async Task OnSuccessShouldReturnValidObject()
        {
            // Arrange
            //
            var faceSettings = TestHelper.GetFaceSettings();

            var handler = new Mock<HttpMessageHandler>();
            var ccResponse = "{\"EUR_PHP\": 51.440375}";
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildCurrencyConverterUrl("EUR", "PHP"))
                .ReturnsResponse(ccResponse, "application/json");

            var client = new CurrencyConverterClient(
                TestHelper.GetLoggerMock<CurrencyConverterClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestCurrencyConverter("EUR", "PHP");

            // Assert
            //
            Assert.Equal(RequestStatusCode.Ok, result.RequestStatus.StatusCode);
            Assert.Equal((decimal) 51.440375, result.ExchangeRate);
        }

        [Fact]
        public async Task OnErrorShouldReturnErrorObject()
        {
            // Arrange
            //
            var faceSettings = TestHelper.GetFaceSettings();

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.BadRequest);

            var client = new CurrencyConverterClient(
                TestHelper.GetLoggerMock<CurrencyConverterClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestCurrencyConverter("EUR", "PHP");

            // Assert
            //
            Assert.Equal(RequestStatusCode.Error, result.RequestStatus.StatusCode);
            Assert.Equal(400, result.RequestStatus.ErrorCode);
        }

        [Fact]
        public async Task OnAuthErrorShouldLogAuthIssue()
        {
            // Arrange
            //
            var loggerMock = TestHelper.GetLoggerMock<CurrencyConverterClient>();
            var faceSettings = TestHelper.GetFaceSettings();

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.Unauthorized);

            var client = new CurrencyConverterClient(
                loggerMock.Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestCurrencyConverter("EUR", "PHP");

            // Assert
            //
            Assert.Equal(RequestStatusCode.Error, result.RequestStatus.StatusCode);
            Assert.Equal(401, result.RequestStatus.ErrorCode);

            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => string.Equals("Unauthorized access to currencyconverterapi.com", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>) It.IsAny<object>()),
                Times.Once);
        }
    }
}
