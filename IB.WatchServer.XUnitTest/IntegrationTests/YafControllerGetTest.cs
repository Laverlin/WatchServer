﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Entity.Settings;
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
    public class YafControllerGetTest : IClassFixture<ServiceAppTestFixture>, IDisposable
    {
        private readonly ServiceAppTestFixture _factory;
        private readonly HttpClient _client;
        private readonly string _lat;
        private readonly string _lon;

        public void Dispose() => _factory.Output = null;

        public YafControllerGetTest(ServiceAppTestFixture factory, ITestOutputHelper output)
        {
            factory.Output = output;
            _factory = factory;

            // Mock database
            //
            var dataProviderMock = new Mock<IDataProvider>();

     

            // Mock web requests
            //
            var faceSettings = _factory.Services.GetRequiredService<FaceSettings>();
            var openWeatherResponse =
                "{\"coord\":{\"lon\":-94.8,\"lat\":38.88},\"weather\":[{\"id\":800,\"main\":\"Clear\",\"description\":\"clear sky\",\"icon\":\"01d\"}],\"base\":\"stations\",\"main\":{\"temp\":4.28,\"feels_like\":0.13,\"temp_min\":3,\"temp_max\":5.56,\"pressure\":1034,\"humidity\":51},\"visibility\":16093,\"wind\":{\"speed\":2.21,\"deg\":169},\"clouds\":{\"all\":1},\"dt\":1584811457,\"sys\":{\"type\":1,\"id\":5188,\"country\":\"US\",\"sunrise\":1584793213,\"sunset\":1584837126},\"timezone\":-18000,\"id\":4276614,\"name\":\"Olathe\",\"cod\":200}";
            var darkSkyResponse =
                "{\"currently\":{\"time\":1584864023,\"summary\":\"Possible Drizzle\",\"icon\":\"rain\",\"precipIntensity\":0.2386,\"precipProbability\":0.4,\"precipType\":\"rain\",\"temperature\":9.39,\"apparentTemperature\":8.3,\"dewPoint\":9.39,\"humidity\":1,\"pressure\":1010.8,\"windSpeed\":2.22,\"windGust\":3.63,\"windBearing\":71,\"cloudCover\":0.52,\"uvIndex\":1,\"visibility\":16.093,\"ozone\":391.9},\"offset\":1}";
            var locationResponse =
                "{\"resourceSets\": [{\"resources\": [{\"name\": \"Olathe, KS\", \"address\": { \"adminDistrict\": \"KS\",\"adminDistrict2\": \"Johnson Co.\",\"countryRegion\": \"United States\",\"formattedAddress\": \"Olathe, KS\",\"locality\": \"Olathe\"}}]}]}";

            _lat = "38.855652";
            _lon = "-94.799712";

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(_lat, _lon))
                .ReturnsResponse(openWeatherResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(_lat, _lon, "fake-key"))
                .ReturnsResponse(darkSkyResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(_lat, _lon, "wrong-key"))
                .ReturnsResponse(HttpStatusCode.Unauthorized);
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildLocationUrl(_lat, _lon))
                .ReturnsResponse(locationResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(null, null))
                .ReturnsResponse(HttpStatusCode.BadRequest);

            var httpFactory = handler.CreateClientFactory();

            // Set Mock services on DI
            //
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
                $"/api/v2/YAFace?apiToken={faceSetting.AuthSettings.Token}&did=test-device10";
            await _client.GetAsync(url);
            var response = await _client.GetAsync(url);

            // Assert
            //
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode); // Status Code 429
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task WrongTokenShouldReturnAuthError()
        {
            // Act
            //
            var url =
                $"/api/v2/YAFace?apiToken=wrong-token&did=test-device11";
            await _client.GetAsync(url);
            var responseWrong = await _client.GetAsync(url);

            // Assert
            //
            Assert.Equal(HttpStatusCode.Forbidden, responseWrong.StatusCode); // Status Code 403
        }

        [Fact]
        public async Task WrongApiVersionShouldReturnError()
        {
            // Act
            //
            var response = await _client.GetAsync($"/api/v1/YAFace");

            // Assert
            //
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync(), typeof(ErrorResponse));
            Assert.IsType<ErrorResponse>(error);
        }


        [Fact]
        public async Task NullRequestShouldReturnEmptyResponse()
        {
            // Arrange
            //
            var expected = new WatchResponse();
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var response = await _client.GetAsync($"/api/v2/YAFace?apiToken={faceSetting.AuthSettings.Token}");

            // Assert
            //
            response.EnsureSuccessStatusCode(); // Status Code 2xx
            Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
        }

    }
}