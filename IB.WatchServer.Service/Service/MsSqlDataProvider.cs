using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using IB.WatchServer.Abstract;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Abstract.Settings;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service
{

    public class MsSqlDataProvider 
    {
        private readonly ILogger<MsSqlDataProvider> _logger;
        private readonly DataConnectionFactory _connectionFactory;
        private readonly IMapper _mapper;


        public MsSqlDataProvider(
            ILogger<MsSqlDataProvider> logger, MsSqlProviderSettings providerSettings, IMapper mapper)
        {
            _logger = logger;
            _connectionFactory = new DataConnectionFactory(providerSettings.GetDataProvider(), providerSettings.BuildConnectionString());
            _mapper = mapper;
        }

        /// <summary>
        /// Store Weather Request and response info in DB
        /// </summary>
        /// <param name="watchRequest">Watch request data</param>
        /// <param name="weatherInfo">Weather data</param>
        /// <param name="locationInfo">Location data</param>
        /// <param name="exchangeRateInfo">Exchange rate data</param>
        public virtual async Task SaveRequestInfo(
            [NotNull] WatchRequest watchRequest, 
            [NotNull] WeatherInfo weatherInfo, 
            [NotNull] LocationInfo locationInfo, 
            [NotNull] ExchangeRateInfo exchangeRateInfo)
        {
            try
            {
                await using var dbWatchServer = _connectionFactory.Create();

                var deviceData = (await dbWatchServer.QueryProcAsync<DeviceData>(
                        "AddDevice",
                        new DataParameter("DeviceID", watchRequest.DeviceId ?? "unknown"),
                        new DataParameter("DeviceName", watchRequest.DeviceName)))
                    .Single();

                var requestData = _mapper.Map<ResponseLog>(watchRequest);
                requestData = _mapper.Map(weatherInfo, requestData);
                requestData = _mapper.Map(locationInfo, requestData);
                requestData = _mapper.Map(exchangeRateInfo, requestData);
                requestData.DeviceDataId = deviceData.Id;
                requestData.RequestTime = DateTime.UtcNow;
            
                await dbWatchServer.GetTable<ResponseLog>().DataContext.InsertAsync(requestData);

                _logger.LogDebug("{@requestInfo}", requestData);
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, "Error MSSql db save");
            }
        }

    }
}

