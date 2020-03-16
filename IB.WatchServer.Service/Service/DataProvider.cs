using System;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using AutoMapper;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Infrastructure;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service
{
    public class DataProvider
    {
        private readonly ILogger<DataProvider> _logger;
        private readonly DataConnectionFactory _dbFactory;
        private readonly IMapper _mapper;
        private readonly IMetrics _metrics;

        public DataProvider(
            ILogger<DataProvider> logger, DataConnectionFactory dbFactory, IMapper mapper, IMetrics metrics)
        {
            _logger = logger;
            _dbFactory = dbFactory;
            _mapper = mapper;
            _metrics = metrics;
        }

        /// <summary>
        /// Store Weather Request and response info in DB
        /// </summary>
        /// <param name="watchFaceRequest">Watch request data</param>
        /// <param name="weatherInfo">Weather data</param>
        /// <param name="locationInfo">Location data</param>
        /// <param name="exchangeRateInfo">Exchange rate data</param>
        public async Task SaveRequestInfo(
            WatchFaceRequest watchFaceRequest, WeatherInfo weatherInfo, LocationInfo locationInfo, ExchangeRateInfo exchangeRateInfo)
        {
            await using var db = _dbFactory.Create();
            var deviceInfo = db.QueryProc<DeviceInfo>(
                    "add_device",
                    new DataParameter("device_id", watchFaceRequest.DeviceId ?? "unknown"),
                    new DataParameter("device_name", watchFaceRequest.DeviceName))
                .Single();

            var requestInfo = _mapper.Map<RequestInfo>(watchFaceRequest);
            requestInfo = _mapper.Map(weatherInfo, requestInfo);
            requestInfo = _mapper.Map(locationInfo, requestInfo);
            if (exchangeRateInfo != null)
                requestInfo = _mapper.Map(exchangeRateInfo, requestInfo);
            requestInfo.DeviceInfoId = deviceInfo.Id;
            requestInfo.RequestTime = DateTime.UtcNow;

            await db.GetTable<RequestInfo>().DataContext.InsertAsync(requestInfo);

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
            _metrics.Measure.Counter.Increment(new CounterOptions {Name = "locationRequest-db", MeasurementUnit = Unit.Calls});
            await using var db = _dbFactory.Create();
            var city = await db.GetTable<RequestInfo>().Where(c => c.RequestTime != null)
                .Join(db.GetTable<DeviceInfo>().Where(d => d.DeviceId == deviceId), c => c.DeviceInfoId, d => d.Id,
                    (c, d) => new {c.CityName, c.Lat, c.Lon, c.RequestTime})
                .OrderByDescending(c => c.RequestTime).Take(1)
                .Where(c => c.Lat == latitude && c.Lon == longitude)
                .SingleOrDefaultAsync();

            return city != null ? new LocationInfo(city.CityName) : null;
        }
    }
}
