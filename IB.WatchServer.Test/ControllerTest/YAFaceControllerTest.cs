using System.Text.Json;
using App.Metrics;
using IB.WatchServer.Service.Controllers;
using IB.WatchServer.Service.Entity.V1;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Service;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IB.WatchServer.Test.ControllerTest
{
    [TestClass]
    public class YAFaceControllerTest
    {
        [TestMethod]
        public void LocationShouldReturnUpgradeMessage()
        {
            // Arrange
            //
            var controller = new YAFaceController(null, null, null, null, null);
            
            var expected = new LocationResponse {CityName = "Update required."};
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var result = controller.Location();
            var resultJson = JsonSerializer.Serialize(result.Value);

            // Arrange
            //
            Assert.AreEqual(expected.CityName, result.Value.CityName);
            Assert.AreEqual(expectedJson, resultJson);
        }

        [TestMethod]
        public void WeatherShouldReturnCorrectWeatherObject()
        {
            // Arrange
            //
            var expected = new WeatherResponse
            {
                WeatherProvider = "OpenWeather",
                Icon = "partly-cloudy-night",
                PrecipProbability = 0,
                Temperature = (decimal) 8.2,
                WindSpeed = (decimal) 6.2,
                Humidity = 1,
                Pressure = 1013,
                CityName = "Olathe, KS"
            };

            var yaFaceProviderMock = new Mock<IYAFaceProvider>();
            yaFaceProviderMock.Setup(_ => _.CheckLastLocation(
                    "4a411568ffc1d40acd84eb51e1296b3ad97dbfe7", (decimal) 38.855652, (decimal) 94.799712))
                .ReturnsAsync("Olathe, KS");
            yaFaceProviderMock.Setup(_ => _.RequestOpenWeather("38.855652", "94.799712"))
                .ReturnsAsync(expected);

            var controller = new YAFaceController(
                Mock.Of<ILogger<YAFaceController>>(), yaFaceProviderMock.Object, null, null, Mock.Of<IMetrics>());

            var watchFaceRequest = new WatchFaceRequest
            {
                WeatherProvider = WeatherProvider.OpenWeather.ToString(),
                DeviceId = "4a411568ffc1d40acd84eb51e1296b3ad97dbfe7",
                DeviceName = "unknown",
                CiqVersion = "1",
                Framework = "1",
                Lat = "38.855652",
                Lon = "94.799712",
                Version = "0.9.180"
            };


            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var result = controller.Weather(watchFaceRequest).Result.Value;
            var resultJson = JsonSerializer.Serialize(result);

            // Assert
            //
            Assert.AreEqual(expected.CityName, result.CityName);
            Assert.AreEqual(expectedJson, resultJson);

        }
    }
}
