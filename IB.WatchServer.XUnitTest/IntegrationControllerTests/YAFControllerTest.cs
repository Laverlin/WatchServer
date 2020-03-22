﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Service;
using IB.WatchServer.XUnitTest.ControllerTests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;
using Xunit.Abstractions;

namespace IB.WatchServer.XUnitTest.IntegrationControllerTests
{
    public class ServiceAppTestFixture : WebApplicationFactory<Service.Program>
    {
        public ITestOutputHelper Output { get; set; }

        // Uses the generic host
        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = base.CreateHostBuilder();

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders(); // Remove other loggers
                logging.AddXUnit(Output); // Use the ITestOutputHelper instance
            });

            return builder;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices((services) =>
            {
                services.RemoveAll(typeof(IHostedService));
            });
        }
    }


    public class YAFControllerTest : IClassFixture<ServiceAppTestFixture>, IDisposable
    {
        private readonly ServiceAppTestFixture _factory;
        private readonly HttpClient _client;

        public void Dispose() => _factory.Output = null;

        public YAFControllerTest(ServiceAppTestFixture factory, ITestOutputHelper output)
        {
            factory.Output = output;
            _factory = factory;
            
            var dataProviderMock = new Mock<IDataProvider>();
            dataProviderMock.Setup(_ => _.CheckLastLocation(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
                .Returns(Task.FromResult("Olathe, KS"));

            var faceSettings = _factory.Services.GetRequiredService<FaceSettings>();
            var openWeatherResponse =
                "{\"coord\":{\"lon\":-94.8,\"lat\":38.88},\"weather\":[{\"id\":800,\"main\":\"Clear\",\"description\":\"clear sky\",\"icon\":\"01d\"}],\"base\":\"stations\",\"main\":{\"temp\":4.28,\"feels_like\":0.13,\"temp_min\":3,\"temp_max\":5.56,\"pressure\":1034,\"humidity\":51},\"visibility\":16093,\"wind\":{\"speed\":2.21,\"deg\":169},\"clouds\":{\"all\":1},\"dt\":1584811457,\"sys\":{\"type\":1,\"id\":5188,\"country\":\"US\",\"sunrise\":1584793213,\"sunset\":1584837126},\"timezone\":-18000,\"id\":4276614,\"name\":\"Olathe\",\"cod\":200}";
            var darkSkyResponse =
                "{\"currently\":{\"time\":1584864023,\"summary\":\"Possible Drizzle\",\"icon\":\"rain\",\"precipIntensity\":0.2386,\"precipProbability\":0.4,\"precipType\":\"rain\",\"temperature\":9.39,\"apparentTemperature\":8.3,\"dewPoint\":9.39,\"humidity\":1,\"pressure\":1010.8,\"windSpeed\":2.22,\"windGust\":3.63,\"windBearing\":71,\"cloudCover\":0.52,\"uvIndex\":1,\"visibility\":16.093,\"ozone\":391.9},\"offset\":1}";
            var locationResponse =
                "{\"resourceSets\": [{\"resources\": [{\"name\": \"Olathe, KS\", \"address\": { \"adminDistrict\": \"KS\",\"adminDistrict2\": \"Johnson Co.\",\"countryRegion\": \"United States\",\"formattedAddress\": \"Olathe, KS\",\"locality\": \"Olathe\"}}]}]}";

            var handler = new Mock<HttpMessageHandler>();
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl("38.855652", "-94.799712"))
                .ReturnsResponse(openWeatherResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl("38.855652", "-94.799712", "fake-key"))
                .ReturnsResponse(darkSkyResponse, "application/json");
            handler.SetupRequest(HttpMethod.Get, faceSettings.BuildLocationUrl("38.855652", "-94.799712"))
                .ReturnsResponse(locationResponse, "application/json");



            var httpFactory = handler.CreateClientFactory();

            _client = _factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {

                        services.AddSingleton(_ => httpFactory);
                        services.AddScoped<IDataProvider>(_ => dataProviderMock.Object);
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
        }

    }
}
