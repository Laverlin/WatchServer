using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Migrations;
using IB.WatchServer.Service.Service;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using IDataProvider = LinqToDB.DataProvider.IDataProvider;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class DataProviderTests
    {
        [Fact(Skip = "db init only")]
     //   [Fact]
        public void RunDbMigration()
        {
            var serviceProvider = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(
                        "User ID=postgres;Password=docker;Host=localhost;Port=5432;Database=WatchService;Pooling=true;")
                    .ScanIn(typeof(BaselineMigration).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddDebug().SetMinimumLevel(LogLevel.Trace))
                .BuildServiceProvider(false);
            serviceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();

        }

        [Fact]
        public async Task SaveDbShouldCorrectlyGetDataFromQuery()
        {
            // Arrange
            //
            var dbParamsMock = new Mock<IDataProvider>();
            var mockDbWatchServer = new Mock<WatchServerDbConnection>(dbParamsMock.Object, "");
            mockDbWatchServer.Setup(_ => _.AddDevice(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new DeviceData {Id = 2});
            var mockFactory = new Mock<DataConnectionFactory<WatchServerDbConnection>>(null, null);

            mockFactory.Setup(_ => _.Create()).Returns(mockDbWatchServer.Object);

            var dataProvider = new DataProvider(
                TestHelper.GetLoggerMock<DataProvider>().Object, mockFactory.Object, MapperConfig.CreateMapper(), null);

            var watchRequest = new WatchRequest
            {
                DeviceId = "device-id",
                DeviceName = "device-name",
                Version = "version",
                CiqVersion = "ciq-version",
                Framework = "framework",
                WeatherProvider = "weather-provider",
                DarkskyKey = "dark-key",
                Lat = (decimal)1.1,
                Lon = (decimal)2.2,
                BaseCurrency = "USD",
                TargetCurrency="EUR"
            };

            // Act
            //
            await dataProvider.SaveRequestInfo(watchRequest, 
                new WeatherInfo{WindSpeed = (decimal) 5.5, Temperature = (decimal) 4.4}, 
                new LocationInfo {CityName = "city-name"}, 
                new ExchangeRateInfo{ExchangeRate = (decimal) 3.3});

            // Assert
            //
            mockDbWatchServer.Verify( _ => _.AddDevice(
                It.Is<string>((o, t) => string.Equals("device-id", o.ToString())), 
                It.Is<string>((o, t) => string.Equals("device-name", o.ToString()))), 
                Times.Once);
            mockDbWatchServer.Verify(_ => _.InsertAsync(It.Is<RequestData>((r, t) =>
                (((RequestData) r).DeviceDataId == 2 &&
                 ((RequestData) r).Version == watchRequest.Version &&
                 ((RequestData) r).CiqVersion == watchRequest.CiqVersion &&
                 ((RequestData) r).Framework == watchRequest.Framework &&
                 ((RequestData) r).Lat == watchRequest.Lat &&
                 ((RequestData) r).Lon == watchRequest.Lon &&
                 ((RequestData) r).BaseCurrency == watchRequest.BaseCurrency &&
                 ((RequestData) r).TargetCurrency == watchRequest.TargetCurrency &&
                 ((RequestData) r).CityName == "city-name" &&
                 ((RequestData) r).ExchangeRate == (decimal) 3.3 &&
                 ((RequestData) r).Temperature == (decimal) 4.4 &&
                 ((RequestData) r).WindSpeed == (decimal) 5.5 )
            )), Times.Once);
        }

        /*
        [Fact]
        public async void LastLocationShouldReturnNullIfLocationDoesNotMatch()
        {
            // Arrange
            //
            var dataMock = ITableSetup(new RequestData
            {
                Id = 4,
                Lon = 0,
                Lat = 0,
                DeviceDataId = 0,
                RequestTime = DateTime.Now
            });

            var dbParamsMock = new Mock<IDataProvider>();
            var mockDbWatchServer = new Mock<WatchServerDbConnection>(dbParamsMock.Object, "");
            mockDbWatchServer.SetupGet(_ => _.RequestData).Returns(dataMock.Object).Verifiable();
            mockDbWatchServer.SetupGet(_ => _.DeviceData)
                .Returns(ITableSetup<DeviceData>(new DeviceData
                {
                    Id = 1,
                    DeviceId = "device-id1",
                    DeviceName = "name1",
                    FirstRequestTime = DateTime.Now
                }).Object).Verifiable();

            var mockFactory = new Mock<DataConnectionFactory<WatchServerDbConnection>>();

            mockFactory.Setup(_ => _.Create()).Returns(mockDbWatchServer.Object);

            var dataProvider = new DataProvider(
                TestHelper.GetLoggerMock<DataProvider>().Object, mockFactory.Object, MapperConfig.CreateMapper(), null);

            // Act
            //
            var result = await dataProvider.LoadLastLocation("device-id", (decimal) 1.1, (decimal) 2.2);

            // Assert
            //
            mockDbWatchServer.VerifyGet(_=>_.DeviceData, Times.Once);
            mockDbWatchServer.VerifyGet(_=>_.RequestData, Times.Once);
            Assert.Null(result);

        }
        */
        private Mock<ITable<T>> ITableSetup<T>(T tValue) where T : class
        {
            IQueryable<T> dummyTable = new List<T> { tValue }.AsQueryable();

            Mock<ITable<T>> tableMock = new Mock<ITable<T>>();
            tableMock.Setup(p => p.GetEnumerator()).Returns(dummyTable.GetEnumerator);
            tableMock.Setup(r => r.Provider).Returns(dummyTable.Provider);
            tableMock.Setup(r => r.ElementType).Returns(dummyTable.ElementType);
            tableMock.Setup(r => r.Expression).Returns(dummyTable.Expression);

            return tableMock;
        }

        private Mock<ITable<T>> ITableSetup<T>() where T : class, new()
        {
            return ITableSetup(new T());
        }

        public interface IDataContextMock : IDataContext
        {
            ITable<T> GetTable<T>() where T : class, new();
        }
    }


}
