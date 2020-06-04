using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IB.WatchServer.Abstract;
using IB.WatchServer.Service.Entity.SailingApp;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Abstract.Settings;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Migrations;
using IB.WatchServer.Service.Service;
using LinqToDB;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using Newtonsoft.Json.Converters;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace IB.WatchServer.XUnitTest.IntegrationTests
{
    [Collection("DB test collection")]
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
            var ccResponse = "{\"BTC_DKK\": 1.2}";

            _lat = (decimal) 38.855652;
            _lon = (decimal)-94.799712;

            _handler = new Mock<HttpMessageHandler>();
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(_lat, _lon))
                .ReturnsResponse(openWeatherResponse, "application/json");
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(_lat, _lon, "test-key"))
                .ReturnsResponse(darkSkyResponse, "application/json");
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildDarkSkyUrl(_lat, _lon, "wrong-key"))
                .ReturnsResponse(HttpStatusCode.Unauthorized);
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildLocationUrl(_lat, _lon))
                .ReturnsResponse(locationResponse, "application/json");
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildOpenWeatherUrl(0, 0))
                .ReturnsResponse(HttpStatusCode.BadRequest);
            _handler.SetupRequest(HttpMethod.Get, faceSettings.BuildCurrencyConverterUrl("BTC", "DKK"))
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
            var url = $"/api/v2/YAFace?apiToken={faceSetting.AuthSettings.Token}&lat={_lat}&lon={_lon}&did={deviceId}&av=0.9.204&fw=5.0&ciqv=3.1.6&dn=unknown&wp=OpenWeather";

            var connectionSettings = _factory.Services.GetRequiredService<IConnectionSettings>();
            var dbConnection = new DataConnectionFactory(connectionSettings.GetDataProvider(), connectionSettings.BuildConnectionString())
                .Create();

            // Act
            //
            var response = await _client.GetAsync(url);

            // Assert
            //
            response.EnsureSuccessStatusCode();
            var actualDevice = await dbConnection.GetTable<DeviceData>().SingleAsync(d => d.DeviceId == deviceId);
            var actualRequest = await dbConnection.GetTable<RequestData>().SingleAsync(r => r.DeviceDataId == actualDevice.Id);

            Assert.Equal("unknown", actualDevice.DeviceName);
            Assert.Equal(deviceId, actualDevice.DeviceId);

            Assert.Equal("5.0", actualRequest.Framework);
            Assert.Equal("3.1.6", actualRequest.CiqVersion);
            Assert.Equal("0.9.204", actualRequest.Version);
            Assert.Equal(_lon, actualRequest.Lon);
            Assert.Equal(_lat, actualRequest.Lat);

            Assert.Equal((decimal) 4.28, actualRequest.Temperature);
            Assert.Equal((decimal) 2.21, actualRequest.WindSpeed);
            Assert.Equal("Olathe, KS", actualRequest.CityName);


            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("api.darksky.net") , Times.Never());
            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("dev.virtualearth.net") , Times.Once());
            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("free.currconv.com") , Times.Never());
            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("api.openweathermap.org") , Times.Once());
            
        }


        [Fact]
        public async void RequestToDarkSkyAndCurrencyShouldStoredInDatabase()
        {
            // Arrange
            //
            var deviceId = "test-device21";
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url = $"/api/v2/YAFace?apiToken={faceSetting.AuthSettings.Token}&lat={_lat}&lon={_lon}&did={deviceId}&av=0.9.204&fw=5.0&ciqv=3.1.6&dn=unknown&wapiKey=test-key&wp=DarkSky&bc=BTC&tc=DKK";

            var connectionSettings = _factory.Services.GetRequiredService<IConnectionSettings>();
            var dbConnection = new DataConnectionFactory(connectionSettings.GetDataProvider(), connectionSettings.BuildConnectionString())
                .Create();

            // Act
            //
            var response = await _client.GetAsync(url);

            // Assert
            //
            response.EnsureSuccessStatusCode();
            var actualDevice = await dbConnection.GetTable<DeviceData>().SingleAsync(d => d.DeviceId == deviceId);
            var actualRequest = await dbConnection.GetTable<RequestData>().SingleAsync(r => r.DeviceDataId == actualDevice.Id);

            Assert.Equal("unknown", actualDevice.DeviceName);
            Assert.Equal(deviceId, actualDevice.DeviceId);

            Assert.Equal("5.0", actualRequest.Framework);
            Assert.Equal("3.1.6", actualRequest.CiqVersion);
            Assert.Equal("0.9.204", actualRequest.Version);
            Assert.Equal(_lon, actualRequest.Lon);
            Assert.Equal(_lat, actualRequest.Lat);
            Assert.Equal("BTC", actualRequest.BaseCurrency);
            Assert.Equal("DKK", actualRequest.TargetCurrency);

            Assert.Equal((decimal) 9.39, actualRequest.Temperature);
            Assert.Equal((decimal) 2.22, actualRequest.WindSpeed);
            Assert.Equal((decimal) 0.4, actualRequest.PrecipProbability);
            Assert.Equal("Olathe, KS", actualRequest.CityName);
            Assert.Equal((decimal) 1.2, actualRequest.ExchangeRate);

            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("free.currconv.com") , Times.Once());
            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("api.darksky.net") , Times.Once());
            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("dev.virtualearth.net") , Times.Once());
            _handler.VerifyRequest(HttpMethod.Get, _ => _.RequestUri.Host.Contains("api.openweathermap.org") , Times.Never());
        }

        [Fact]
        public async Task SaveDbShouldCorrectlyGetDataFromQuery()
        {
            // Arrange
            //

            var connectionSettings = _factory.Services.GetRequiredService<IConnectionSettings>();
            var dbConnection = new DataConnectionFactory(connectionSettings.GetDataProvider(), connectionSettings.BuildConnectionString())
                .Create();

            var dataProvider = new PostgresDataProvider(
                TestHelper.GetLoggerMock<PostgresDataProvider>().Object, 
                new DataConnectionFactory(connectionSettings), 
                MapperConfig.CreateMapper(), null);

            var watchRequest = new WatchRequest
            {
                DeviceId = "device-id",
                DeviceName = "device-name",
                Version = "version",
                CiqVersion = "ciq-version",
                Framework = "framework",
                WeatherProvider = "weather-provider",
                DarkskyKey = "dark-key",
                Lat = (decimal) 1.1,
                Lon = (decimal) 2.2,
                BaseCurrency = "USD",
                TargetCurrency = "EUR"
            };

            // Act
            //
            await dataProvider.SaveRequestInfo(watchRequest,
                new WeatherInfo {WindSpeed = (decimal) 5.5, Temperature = (decimal) 4.4},
                new LocationInfo {CityName = "city-name"},
                new ExchangeRateInfo {ExchangeRate = (decimal) 3.3});

            // Assert
            //
            var actualDevice = await dbConnection.GetTable<DeviceData>().SingleAsync(d => d.DeviceId == watchRequest.DeviceId);
            var actualRequest = await dbConnection.GetTable<RequestData>().SingleAsync(r => r.DeviceDataId == actualDevice.Id);


            Assert.Equal("device-name", actualDevice.DeviceName);
            Assert.Equal("device-id", actualDevice.DeviceId);


            Assert.Equal(watchRequest.Framework, actualRequest.Framework);
            Assert.Equal(watchRequest.CiqVersion, actualRequest.CiqVersion);
            Assert.Equal(watchRequest.Version, actualRequest.Version);
            Assert.Equal(watchRequest.Lon, actualRequest.Lon);
            Assert.Equal(watchRequest.Lat, actualRequest.Lat);
            Assert.Equal(watchRequest.BaseCurrency, actualRequest.BaseCurrency);
            Assert.Equal(watchRequest.TargetCurrency, actualRequest.TargetCurrency);

            Assert.Equal((decimal) 4.4, actualRequest.Temperature);
            Assert.Equal("city-name", actualRequest.CityName);
            Assert.Equal((decimal) 3.3, actualRequest.ExchangeRate);

        }
        
        [Fact]
        public async Task SameDeviceWithTheSameCoordinatesShouldReturnCity()
        {
            // Arrange
            //
            var connectionSettings = _factory.Services.GetRequiredService<IConnectionSettings>();

            var dataProvider = new PostgresDataProvider(
                TestHelper.GetLoggerMock<PostgresDataProvider>().Object, 
                new DataConnectionFactory(connectionSettings), 
                MapperConfig.CreateMapper(), 
                TestHelper.GetMetricsMock().Object);

            var watchRequest = new WatchRequest
            {
                DeviceId = "device-id",
                DeviceName = "device-name",
                Version = "version",
                CiqVersion = "ciq-version",
                Framework = "framework",
                WeatherProvider = "weather-provider",
                DarkskyKey = "dark-key",
                Lat = (decimal) 1.1,
                Lon = (decimal) 2.2,
                BaseCurrency = "USD",
                TargetCurrency = "EUR"
            };


            await dataProvider.SaveRequestInfo(watchRequest,
                new WeatherInfo {WindSpeed = (decimal) 5.5, Temperature = (decimal) 4.4},
                new LocationInfo {CityName = "city-name2"},
                new ExchangeRateInfo {ExchangeRate = (decimal) 3.3});


            // Act
            //
            var actualLocationInfo = await dataProvider.LoadLastLocation("device-id", (decimal)1.1, (decimal)2.2);

            // Assert
            //
            Assert.Equal("city-name2", actualLocationInfo.CityName);
            Assert.Equal(RequestStatusCode.Ok, actualLocationInfo.RequestStatus.StatusCode);
        }

        [Fact]
        public async Task NoLocationShouldReturnNull()
        {
            // Arrange
            //
            var connectionSettings = _factory.Services.GetRequiredService<IConnectionSettings>();

            var dataProvider = new PostgresDataProvider(
                TestHelper.GetLoggerMock<PostgresDataProvider>().Object, 
                new DataConnectionFactory(connectionSettings), 
                MapperConfig.CreateMapper(), 
                TestHelper.GetMetricsMock().Object);

            // Act
            //
            var actualLocationInfo = await dataProvider.LoadLastLocation("device-id", (decimal)1.1, (decimal)2.2);

            // Assert
            //
            Assert.Null(actualLocationInfo);
        }

        [Theory]
        [InlineData("/api/v1/YASail/RouteList/")]
        [InlineData("/api/v2/YASail/RouteList/")]
        public async Task RoutesRequestShouldReturnRoutes(string baseUrl)
        {
            // Arrange
            //
            var time = DateTime.Now;

            var connectionSettings = _factory.Services.GetRequiredService<IConnectionSettings>();
            var dbConnection = new DataConnectionFactory(connectionSettings.GetDataProvider(), connectionSettings.BuildConnectionString())
                .Create();
            var publicId = "testiddd";
            var yasUser = new YasUser {PublicId = publicId, TelegramId = 0};
            var userId = await dbConnection.GetTable<YasUser>().DataContext.InsertWithInt64IdentityAsync(yasUser);
            var yasRoute = new YasRoute {RouteName = "test-name", UserId = userId, UploadTime = time.ToUniversalTime()};
            var routeId = await dbConnection.GetTable<YasRoute>().DataContext.InsertWithInt64IdentityAsync(yasRoute);
            var wayPoint = new YasWaypoint {Latitude = 1, Longitude = 2, Name = "wp-1", OrderId = 0, RouteId = routeId};
            await dbConnection.GetTable<YasWaypoint>().DataContext.InsertWithInt64IdentityAsync(wayPoint);
            
            var faceSetting = _factory.Services.GetRequiredService<FaceSettings>();
            var url = $"{baseUrl}{publicId}";


            // Act
            //
            var response = await _client.GetAsync(url);

            // Assert
            //
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var expected =
                "[{\"routeId\":1,\"userId\":1,\"RouteName\":\"test-name\",\"RouteDate\":\"" + 
                time.ToString("yyyy-MM-ddTHH\\:mm\\:ss.FFFFFFzzz")  + 
                "\",\"WayPoints\":[{\"waypointId\":1,\"routeId\":1,\"name\":\"wp-1\",\"Lat\":1,\"Lon\":2}]}]";
            Assert.Equal(expected, result);

        }

    }
}
