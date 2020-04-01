using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Service.HttpClients;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class VirtualearthClientTest
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
            var locationResponse =
                "{\"resourceSets\": [{\"resources\": [{\"name\": \"Olathe, KS\", \"address\": { \"adminDistrict\": \"KS\",\"adminDistrict2\": \"Johnson Co.\",\"countryRegion\": \"United States\",\"formattedAddress\": \"Olathe, KS\",\"locality\": \"Olathe\"}}]}]}";
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildLocationUrl(lat, lon))
                .ReturnsResponse(locationResponse, "application/json");

            var client = new VirtualearthClient(
                TestHelper.GetLoggerMock<VirtualearthClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestLocationName(lat, lon);

            // Assert
            //
            Assert.Equal(RequestStatusCode.Ok, result.RequestStatus.StatusCode);
            Assert.Equal("Olathe, KS", result.CityName);
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

            var client = new VirtualearthClient(
                TestHelper.GetLoggerMock<VirtualearthClient>().Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestLocationName(lat, lon);

            // Assert
            //
            Assert.Equal(RequestStatusCode.Error, result.RequestStatus.StatusCode);
            Assert.Equal(400, result.RequestStatus.ErrorCode);
            Assert.Null(result.CityName);
        }

        [Fact]
        public async Task OnAuthErrorShouldLogAuthIssue()
        {
            // Arrange
            //
            var faceSettings = TestHelper.GetFaceSettings();
            var lat = (decimal) 38.855652;
            var lon = (decimal) -94.799712;

            var loggerMock = TestHelper.GetLoggerMock<VirtualearthClient>();

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.Unauthorized);

            var client = new VirtualearthClient(
                loggerMock.Object,
                handler.CreateClient(),
                faceSettings,
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var result = await client.RequestLocationName(lat, lon);

            // Assert
            //
            Assert.Equal(RequestStatusCode.Error, result.RequestStatus.StatusCode);
            Assert.Equal(401, result.RequestStatus.ErrorCode);
            Assert.Null(result.CityName);
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => string.Equals("Unauthorized access to virtualearth", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>) It.IsAny<object>()),
                Times.Once);
        }
    }
}
