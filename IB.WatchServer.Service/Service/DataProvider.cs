using System;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using AutoMapper;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Entity.Settings;
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
        private readonly FaceSettings _faceSettings;
        private readonly DataConnectionFactory _dbFactory;
        private readonly IMapper _mapper;
        private readonly IMetrics _metrics;

        public DataProvider(
            ILogger<DataProvider> logger, FaceSettings faceSettings, DataConnectionFactory dbFactory, IMapper mapper, IMetrics metrics)
        {
            _logger = logger;
            _faceSettings = faceSettings;
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
        public async Task SaveRequestInfo(WatchFaceRequest watchFaceRequest, WeatherInfo weatherInfo, LocationInfo locationInfo)
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
            requestInfo.DeviceInfoId = deviceInfo.Id;
            requestInfo.RequestTime = DateTime.UtcNow;

            await db.GetTable<RequestInfo>().DataContext.InsertAsync(requestInfo);

            _logger.LogDebug("{@requestInfo}", requestInfo);
        }
    }
}
