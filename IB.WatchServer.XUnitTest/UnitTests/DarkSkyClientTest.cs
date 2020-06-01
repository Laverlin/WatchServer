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
    public class DarkSkyClientTest
    {
        [Fact]
        public async Task OnSuccessShouldReturnValidObject()
        {
            // Arrange
            //
            var faceSettings = TestHelper.GetFaceSettings();
            var lat = (decimal) 38.855652;
            var lon = (decimal) -94.799712;
            var token = "test-token";

            var handler = new Mock<HttpMessageHandler>();
            var darkSkyResponse =
                "{\"currently\":{\"time\":1584864023,\"summary\":\"Possible Drizzle\",\"icon\":\"rain\",\"precipIntensity\":0.2386,\"precipProbability\":0.4,\"precipType\":\"rain\",\"temperature\":9.39,\"apparentTemperature\":8.3,\"dewPoint\":9.39,\"humidity\":1,\"pressure\":1010.8,\"windSpeed\":2.22,\"windGust\":3.63,\"windBearing\":71,\"cloudCover\":0.52,\"uvIndex\":1,\"visibility\":16.093,\"ozone\":391.9},\"offset\":1}";
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(lat, lon, token))
                .ReturnsResponse(darkSkyResponse, "application/json");

            var client = new DarkSkyClient(
                TestHelper.GetLoggerMock<DarkSkyClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestDarkSky(lat, lon, token);

            // Assert
            //
            Assert.Equal(RequestStatusCode.Ok, result.RequestStatus.StatusCode);
            Assert.Equal("rain", result.Icon);
            Assert.Equal((decimal)0.4, result.PrecipProbability);
            Assert.Equal((decimal)9.39, result.Temperature);
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

            var client = new DarkSkyClient(
                TestHelper.GetLoggerMock<DarkSkyClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestDarkSky(lat, lon, "test-token");

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

            var loggerMock = TestHelper.GetLoggerMock<DarkSkyClient>();

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.Unauthorized);

            var client = new DarkSkyClient(
                loggerMock.Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestDarkSky(lat, lon, "test-token");

            // Assert
            //
            Assert.Equal(RequestStatusCode.Error, result.RequestStatus.StatusCode);
            Assert.Equal(401, result.RequestStatus.ErrorCode);

            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => string.Equals("Unauthorized access to DarkSky", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>) It.IsAny<object>()),
                Times.Once);
        }

    }
}
