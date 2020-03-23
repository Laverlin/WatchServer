﻿using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.V1;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Service;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;
using Xunit.Abstractions;

namespace IB.WatchServer.XUnitTest.IntegrationTests
{
    public class YafControllerTest : IClassFixture<ServiceAppTestFixture>, IDisposable
    {
        private readonly ServiceAppTestFixture _factory;
        private readonly HttpClient _client;
        private WatchFaceRequest _watchFaceRequest;

        public void Dispose() => _factory.Output = null;

        public YafControllerTest(ServiceAppTestFixture factory, ITestOutputHelper output)
        {
            factory.Output = output;
            _factory = factory;
            
            var dataProviderMock = new Mock<IDataProvider>();
            dataProviderMock.Setup(_ => _.CheckLastLocation(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
                .Returns(Task.FromResult("Olathe, KS"));
            dataProviderMock.Setup(_ => _.SaveRequestInfo(
                    It.IsAny<RequestType>(), It.IsAny<WatchFaceRequest>(), It.IsAny<Service.Entity.V1.WeatherResponse>()))
                .Callback<RequestType, WatchFaceRequest, Service.Entity.V1.WeatherResponse>((rt, wfr, wr) => _watchFaceRequest = wfr);

            var faceSettings = _factory.Services.GetRequiredService<FaceSettings>();
            var openWeatherResponse =
                "{\"coord\":{\"lon\":-94.8,\"lat\":38.88},\"weather\":[{\"id\":800,\"main\":\"Clear\",\"description\":\"clear sky\",\"icon\":\"01d\"}],\"base\":\"stations\",\"main\":{\"temp\":4.28,\"feels_like\":0.13,\"temp_min\":3,\"temp_max\":5.56,\"pressure\":1034,\"humidity\":51},\"visibility\":16093,\"wind\":{\"speed\":2.21,\"deg\":169},\"clouds\":{\"all\":1},\"dt\":1584811457,\"sys\":{\"type\":1,\"id\":5188,\"country\":\"US\",\"sunrise\":1584793213,\"sunset\":1584837126},\"timezone\":-18000,\"id\":4276614,\"name\":\"Olathe\",\"cod\":200}";
            var darkSkyResponse =
                "{\"currently\":{\"time\":1584864023,\"summary\":\"Possible Drizzle\",\"icon\":\"rain\",\"precipIntensity\":0.2386,\"precipProbability\":0.4,\"precipType\":\"rain\",\"temperature\":9.39,\"apparentTemperature\":8.3,\"dewPoint\":9.39,\"humidity\":1,\"pressure\":1010.8,\"windSpeed\":2.22,\"windGust\":3.63,\"windBearing\":71,\"cloudCover\":0.52,\"uvIndex\":1,\"visibility\":16.093,\"ozone\":391.9},\"offset\":1}";
            var locationResponse =
                "{\"resourceSets\": [{\"resources\": [{\"name\": \"Olathe, KS\", \"address\": { \"adminDistrict\": \"KS\",\"adminDistrict2\": \"Johnson Co.\",\"countryRegion\": \"United States\",\"formattedAddress\": \"Olathe, KS\",\"locality\": \"Olathe\"}}]}]}";

            var lat = "38.855652";
            var lon = "-94.799712";

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(lat, lon))
                .ReturnsResponse(openWeatherResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(lat, lon, "fake-key"))
                .ReturnsResponse(darkSkyResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(lat, lon, "wrong-key"))
                .ReturnsResponse(HttpStatusCode.Unauthorized);
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildLocationUrl(lat, lon))
                .ReturnsResponse(locationResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(null, null))
                .ReturnsResponse(HttpStatusCode.BadRequest);

            var httpFactory = handler.CreateClientFactory();

            _client = _factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddSingleton(_ => httpFactory);
                        services.AddScoped(_ => dataProviderMock.Object);
                    });
                    
                })
                .CreateClient();
        }

        [Theory]
        [InlineData("/api/v1/YAFace/Location")]
        public async Task LocationShouldReturnUpdateMessage(string url)
        {
            // Arrange
            //
            var expected = new LocationResponse {CityName = "Update required."};
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var response = await _client.GetAsync(url);

            // Assert
            //
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task WeatherShouldReturnOpenWeatherResponse()
        {
            // Arrange
            //
            var expected = new WeatherResponse
            {
                WeatherProvider = "OpenWeather",
                Icon = "clear-day",
                PrecipProbability = 0,
                Temperature = (decimal) 4.28,
                WindSpeed = (decimal) 2.21,
                Humidity = (decimal) 0.51,
                Pressure = 1034,
                CityName = "Olathe, KS"
            };
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url = $"/api/v1/YAFace/weather?apiToken={faceSetting.AuthSettings.Token}&lat=38.855652&lon=-94.799712&did=test-device2&v=0.9.204&fw=5.0&ciqv=3.1.6&dname=unknown&wp=OpenWeather";
            var response = await _client.GetAsync(url);

            // Assert
            //
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
            Assert.Equal("unknown", _watchFaceRequest.DeviceName);
            Assert.Equal("0.9.204", _watchFaceRequest.Version);
            Assert.Equal("5.0", _watchFaceRequest.Framework);
            Assert.Equal("OpenWeather", _watchFaceRequest.WeatherProvider);
            Assert.Equal("test-device2", _watchFaceRequest.DeviceId);
        }

        [Fact]
        public async Task WeatherShouldReturnDarkSkyResponse()
        {
            // Arrange
            //
            var expected = new WeatherResponse
            {
                WeatherProvider = "DarkSky",
                Icon = "rain",
                PrecipProbability = (decimal) 0.4,
                Temperature = (decimal) 9.39,
                WindSpeed = (decimal) 2.22,
                Humidity = (decimal) 1,
                Pressure = (decimal) 1010.8,
                CityName = "Olathe, KS"
            };
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url =
                $"/api/v1/YAFace/weather?apiToken={faceSetting.AuthSettings.Token}&did=test-device1&dname=unknown&v=0.9.208&lat=38.855652&lon=-94.799712&wapiKey=fake-key&wp=DarkSky&fw=5.0&ciqv=3.1.6";
            var response = await _client.GetAsync(url);

            // Assert
            //
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
            Assert.Equal("unknown", _watchFaceRequest.DeviceName);
            Assert.Equal("0.9.208", _watchFaceRequest.Version);
            Assert.Equal("5.0", _watchFaceRequest.Framework);
            Assert.Equal("DarkSky", _watchFaceRequest.WeatherProvider);
            Assert.Equal("fake-key", _watchFaceRequest.DarkskyKey);
            Assert.Equal("test-device1", _watchFaceRequest.DeviceId);

        }

        [Fact]
        public async Task DarkSkyRequestWithWrongKeyShouldReturn403()
        {
            // Arrange
            //
            var expected = new ErrorResponse
            {
                StatusCode = 403,
                Description = "Forbidden"
            };
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url =
                $"/api/v1/YAFace/weather?apiToken={faceSetting.AuthSettings.Token}&did=test-device3&dname=unknown&v=0.9.208&lat=38.855652&lon=-94.799712&wapiKey=wrong-key&wp=DarkSky&fw=5.0&ciqv=3.1.6";
            var response = await _client.GetAsync(url);

            // Assert
            //
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // Status Code 403
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task NullRequestShouldReturn400()
        {
            // Arrange
            //
            var expected = new ErrorResponse
            {
                StatusCode = 400,
                Description = "Bad request"
            };
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url =
                $"/api/v1/YAFace/weather?apiToken={faceSetting.AuthSettings.Token}&did=test-device4";
            var response = await _client.GetAsync(url);

            // Assert
            //
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // Status Code 400
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task NextRequestWithTheSameDeviceWithin5SecShouldReturn429()
        {
            // Arrange
            //
            var expected = new ErrorResponse
            {
                StatusCode = 429,
                Description = "Too many requests, retry after 5"
            };
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url =
                $"/api/v1/YAFace/weather?apiToken={faceSetting.AuthSettings.Token}&did=test-device5";
            await _client.GetAsync(url);
            var response = await _client.GetAsync(url);

            // Assert
            //
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode); // Status Code 429
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
        }
    }
}
