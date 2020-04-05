using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Migrations;
using IB.WatchServer.Service.Service;
using LinqToDB;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;
using Xunit.Abstractions;

namespace IB.WatchServer.XUnitTest.IntegrationTests
{
    public class YafControllerFullTest  : IClassFixture<ServiceAppTestFixture>, IDisposable
    {
        private readonly ServiceAppTestFixture _factory;
        private readonly HttpClient _client;
        private readonly decimal _lat;
        private readonly decimal _lon;
        private readonly Mock<HttpMessageHandler> _handler;
        private readonly MigrationRunner _migrationRunner;

        public void Dispose()
        {
            _migrationRunner.RunMigrationDown(new BaselineMigration());
            _factory.Output = null;
        }

        public YafControllerFullTest(ServiceAppTestFixture factory, ITestOutputHelper output)
        {

            factory.Output = output;
            _factory = factory;

            // Prepare Database
            //
            var config = _factory.Services.GetRequiredService<IConnectionSettings>();
            _migrationRunner = new MigrationRunner(config.BuildConnectionString());
            _migrationRunner.RunMigrationsUp();

            // Mock web requests
            //
            var faceSettings = _factory.Services.GetRequiredService<FaceSettings>();
            var openWeatherResponse =
                "{\"coord\":{\"lon\":-94.8,\"lat\":38.88},\"weather\":[{\"id\":800,\"main\":\"Clear\",\"description\":\"clear sky\",\"icon\":\"01d\"}],\"base\":\"stations\",\"main\":{\"temp\":4.28,\"feels_like\":0.13,\"temp_min\":3,\"temp_max\":5.56,\"pressure\":1034,\"humidity\":51},\"visibility\":16093,\"wind\":{\"speed\":2.21,\"deg\":169},\"clouds\":{\"all\":1},\"dt\":1584811457,\"sys\":{\"type\":1,\"id\":5188,\"country\":\"US\",\"sunrise\":1584793213,\"sunset\":1584837126},\"timezone\":-18000,\"id\":4276614,\"name\":\"Olathe\",\"cod\":200}";
            var darkSkyResponse =
                "{\"currently\":{\"time\":1584864023,\"summary\":\"Possible Drizzle\",\"icon\":\"rain\",\"precipIntensity\":0.2386,\"precipProbability\":0.4,\"precipType\":\"rain\",\"temperature\":9.39,\"apparentTemperature\":8.3,\"dewPoint\":9.39,\"humidity\":1,\"pressure\":1010.8,\"windSpeed\":2.22,\"windGust\":3.63,\"windBearing\":71,\"cloudCover\":0.52,\"uvIndex\":1,\"visibility\":16.093,\"ozone\":391.9},\"offset\":1}";
            var locationResponse =
                "{\"resourceSets\": [{\"resources\": [{\"name\": \"Olathe, KS\", \"address\": { \"adminDistrict\": \"KS\",\"adminDistrict2\": \"Johnson Co.\",\"countryRegion\": \"United States\",\"formattedAddress\": \"Olathe, KS\",\"locality\": \"Olathe\"}}]}]}";
            var ccResponse = "{\"USD_DKK\": 1.1}";

            _lat = (decimal) 38.855652;
            _lon = (decimal)-94.799712;

            _handler = new Mock<HttpMessageHandler>();
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(_lat, _lon))
                .ReturnsResponse(openWeatherResponse, "application/json");
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(_lat, _lon, "fake-key"))
                .ReturnsResponse(darkSkyResponse, "application/json");
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(_lat, _lon, "wrong-key"))
                .ReturnsResponse(HttpStatusCode.Unauthorized);
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildLocationUrl(_lat, _lon))
                .ReturnsResponse(locationResponse, "application/json");
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(0, 0))
                .ReturnsResponse(HttpStatusCode.BadRequest);
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildCurrencyConverterUrl("USD", "DKK"))
                .ReturnsResponse(ccResponse, "application/json");


            var httpFactory = _handler.CreateClientFactory();

            // Set Mock services on DI
            //
            _client = _factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services => { services.AddSingleton(_ => httpFactory); });
                })
                .CreateClient();
        }

        [Fact]
        public async void RequestToOpenWeatherShouldStoredInDatabase()
        {
            // Arrange
            //
            var deviceId = "test-device20";
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url = $"/api/v1/YAFace/weather?apiToken={faceSetting.AuthSettings.Token}&lat={_lat}&lon={_lon}&did={deviceId}&v=0.9.204&fw=5.0&ciqv=3.1.6&dname=unknown&wp=OpenWeather";

            var connectionSettings = _factory.Services.GetRequiredService<IConnectionSettings>();
            var dbConnection = new WatchServerDbConnection(connectionSettings.GetDataProvider(), connectionSettings.BuildConnectionString());

            // Act
            //
            var response = await _client.GetAsync(url);

            // Assert
            //
            response.EnsureSuccessStatusCode();
            var actualDevice = await dbConnection.DeviceData.SingleAsync(d => d.DeviceId == deviceId);
            var actualRequest = await dbConnection.RequestData.SingleAsync(r => r.DeviceDataId == actualDevice.Id);

            Assert.Equal((decimal) 4.28, actualRequest.Temperature);
        }
    }
}
