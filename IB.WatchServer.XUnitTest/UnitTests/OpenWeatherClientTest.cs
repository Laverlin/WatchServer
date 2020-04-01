using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Service.HttpClients;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class OpenWeatherClientTest
    {
        [Fact]
        public async Task OnSuccessShouldReturnValidObject()
        {
            // Arrange
            //
            var faceSettings = TestHelper.GetFaceSettings();
            var lat = (decimal) 38.855652;
            var lon = (decimal) -94.799712;

            var handler = new Mock<HttpMessageHandler>();
            var openWeatherResponse =
                "{\"coord\":{\"lon\":-94.8,\"lat\":38.88},\"weather\":[{\"id\":800,\"main\":\"Clear\",\"description\":\"clear sky\",\"icon\":\"01d\"}],\"base\":\"stations\",\"main\":{\"temp\":4.28,\"feels_like\":0.13,\"temp_min\":3,\"temp_max\":5.56,\"pressure\":1034,\"humidity\":51},\"visibility\":16093,\"wind\":{\"speed\":2.21,\"deg\":169},\"clouds\":{\"all\":1},\"dt\":1584811457,\"sys\":{\"type\":1,\"id\":5188,\"country\":\"US\",\"sunrise\":1584793213,\"sunset\":1584837126},\"timezone\":-18000,\"id\":4276614,\"name\":\"Olathe\",\"cod\":200}";
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(lat, lon))
                .ReturnsResponse(openWeatherResponse, "application/json");

            var client = new OpenWeatherClient(
                TestHelper.GetLoggerMock<OpenWeatherClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object,
                MapperConfig.CreateMapper());

            // Act
            //
            var result = await client.RequestOpenWeather(lat, lon);

            // Assert
            //
            Assert.Equal(RequestStatusCode.Ok, result.RequestStatus.StatusCode);
            Assert.Equal("clear-day", result.Icon);
            Assert.Equal((decimal)0.51, result.Humidity);
            Assert.Equal((decimal)4.28, result.Temperature);
        }

        [Fact]
        public async Task OnErrorShouldReturnErrorObject()
        {
            // Arrange
            //
            var faceSettings = TestHelper.GetFaceSettings();
            var lat = (decimal) 38.855652;
            var lon = (decimal) -94.799712;

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.BadRequest);

            var client = new OpenWeatherClient(
                TestHelper.GetLoggerMock<OpenWeatherClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object,
                MapperConfig.CreateMapper());

            // Act
            //
            var result = await client.RequestOpenWeather(lat, lon);

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
            var faceSettings = TestHelper.GetFaceSettings();
            var lat = (decimal) 38.855652;
            var lon = (decimal) -94.799712;

            var loggerMock = TestHelper.GetLoggerMock<OpenWeatherClient>();

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.Unauthorized);

            var client = new OpenWeatherClient(
                loggerMock.Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object,
                MapperConfig.CreateMapper());

            // Act
            //
            var result = await client.RequestOpenWeather(lat, lon);

            // Assert
            //
            Assert.Equal(RequestStatusCode.Error, result.RequestStatus.StatusCode);
            Assert.Equal(401, result.RequestStatus.ErrorCode);

            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => string.Equals("Unauthorized access to OpenWeather", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>) It.IsAny<object>()),
                Times.Once);
        }

    }
}
