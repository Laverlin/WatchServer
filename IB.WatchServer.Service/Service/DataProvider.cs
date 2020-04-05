using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using AutoMapper;

using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Infrastructure;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service
{

    public class WatchServerDbConnection : DataConnection
    {
        public WatchServerDbConnection(LinqToDB.DataProvider.IDataProvider dataProvider, string connectionString) : 
            base(dataProvider, connectionString){}

        public virtual ITable<DeviceData> DeviceData => GetTable<DeviceData>();

        public virtual ITable<RequestData> RequestData => GetTable<RequestData>();

        public virtual Task<int> InsertAsync(RequestData requestData)
        {
            return RequestData.DataContext.InsertAsync(requestData);
        }

        public virtual DeviceData AddDevice(string deviceId, string deviceName)
        {
            return this.QueryProc<DeviceData>(
                    "add_device",
                    new DataParameter("device_id", deviceId),
                    new DataParameter("device_name", deviceName))
                .Single();
        }
    }

    public class DataProvider : IDataProvider
    {
        private readonly ILogger<DataProvider> _logger;
        private readonly DataConnectionFactory<WatchServerDbConnection> _connectionFactory;
        private readonly IMapper _mapper;
        private readonly IMetrics _metrics;

        public DataProvider(
            ILogger<DataProvider> logger, DataConnectionFactory<WatchServerDbConnection> connectionFactory, IMapper mapper, IMetrics metrics)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _mapper = mapper;
            _metrics = metrics;
        }

        /// <summary>
        /// Store Weather Request and response info in DB
        /// </summary>
        /// <param name="watchRequest">Watch request data</param>
        /// <param name="weatherInfo">Weather data</param>
        /// <param name="locationInfo">Location data</param>
        /// <param name="exchangeRateInfo">Exchange rate data</param>
        public async Task SaveRequestInfo(
            [NotNull] WatchRequest watchRequest, 
            [NotNull] WeatherInfo weatherInfo, 
            [NotNull] LocationInfo locationInfo, 
            [NotNull] ExchangeRateInfo exchangeRateInfo)
        {

            await using var dbWatchServer = _connectionFactory.Create();
            var deviceData = dbWatchServer.AddDevice(watchRequest.DeviceId ?? "unknown", watchRequest.DeviceName);

            var requestInfo = _mapper.Map<RequestData>(watchRequest);
            requestInfo = _mapper.Map(weatherInfo, requestInfo);
            requestInfo = _mapper.Map(locationInfo, requestInfo);
            requestInfo = _mapper.Map(exchangeRateInfo, requestInfo);
            requestInfo.DeviceDataId = deviceData.Id;
            requestInfo.RequestTime = DateTime.UtcNow;
            
            await dbWatchServer.InsertAsync(requestInfo);

            _logger.LogDebug("{@requestInfo}", requestInfo);
        }

        /// <summary>
        /// Search in DB the last location of this device. If location is the same the LocationInfo with City name will be returned,
        /// otherwise null
        /// </summary>
        /// <param name="deviceId">Garmin device id</param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns><see cref="LocationInfo"/> with CityName or null</returns>
        public async Task<LocationInfo> LoadLastLocation(string deviceId, decimal latitude, decimal longitude)
        {
            await using var dbWatchServer = _connectionFactory.Create();

            var city = await dbWatchServer.RequestData.Where(c => c.RequestTime != null)
                .Join(dbWatchServer.DeviceData.Where(d => d.DeviceId == deviceId), c => c.DeviceDataId, d => d.Id,
                    (c, d) => new {c.CityName, c.Lat, c.Lon, c.RequestTime})
                .OrderByDescending(c => c.RequestTime).Take(1)
                .Where(c => c.Lat == latitude && c.Lon == longitude)
                .SingleOrDefaultAsync();

            if (city == null) return null;

            _metrics.LocationIncrement("cache", SourceType.Database);
            return new LocationInfo(city.CityName);
        }
    }
}
