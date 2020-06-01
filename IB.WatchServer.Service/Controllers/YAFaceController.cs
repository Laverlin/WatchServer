using System;
using System.Net;
using System.Threading.Tasks;
using IB.WatchServer.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

using IB.WatchServer.Abstract.Entity;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Service.Service.HttpClients;
using LinqToDB.Common;

namespace IB.WatchServer.Service.Controllers
{
    /// <summary>
    /// Controller for the watch face requests
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true), ApiVersion("2.0")]
    public class YAFaceController : ControllerBase
    {
        private readonly ILogger<YAFaceController> _logger;
        private readonly PostgresDataProvider _postgresDataProvider;
        private readonly KafkaProvider _kafkaProvider;
        private readonly ExchangeRateCacheStrategy _exchangeRateCacheStrategy;
        private readonly VirtualearthClient _virtualearthClient;
        private readonly DarkSkyClient _darkSkyClient;
        private readonly OpenWeatherClient _openWeatherClient;

        public YAFaceController(
            ILogger<YAFaceController> logger, PostgresDataProvider postgresDataProvider, KafkaProvider kafkaProvider,
            ExchangeRateCacheStrategy exchangeRateCacheStrategy,
            VirtualearthClient virtualearthClient,  
            DarkSkyClient darkSkyClient,
            OpenWeatherClient openWeatherClient)
        {
            _logger = logger;
            _postgresDataProvider = postgresDataProvider;
            _kafkaProvider = kafkaProvider;
            _exchangeRateCacheStrategy = exchangeRateCacheStrategy;
            _virtualearthClient = virtualearthClient;
            _darkSkyClient = darkSkyClient;
            _openWeatherClient = openWeatherClient;
        }

        /// <summary>
        /// Process request of the location
        /// </summary>
        [HttpGet("location"), MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField ="did", Seconds = 5)]
        public ActionResult<object> Location()
        {
            return new
            {
                CityName = "Update required.",
                ServerVersion = SolutionInfo.Version
            };
        }

        /// <summary>
        /// Provide weather info
        /// </summary>
        /// <param name="watchFaceRequest"></param>
        /// <returns>The data of current weather in given location</returns>
        [HttpGet("weather"), MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField = "did", Seconds = 5)]
        [Authorize]
        public async Task<ActionResult<object>> Weather([FromQuery] WatchRequest watchFaceRequest)
        {

            watchFaceRequest.Version = Request.Query["v"];
            watchFaceRequest.DeviceName = Request.Query["dname"];

            var result = await Get(watchFaceRequest);
            var watchResponse = result.Value;

            if (watchResponse != null && 
                watchResponse.WeatherInfo.RequestStatus.StatusCode == RequestStatusCode.Error &&
                watchResponse.WeatherInfo.RequestStatus.ErrorCode == 401)
            {
                return StatusCode((int) HttpStatusCode.Forbidden,
                    new ErrorResponse {StatusCode = (int) HttpStatusCode.Forbidden, Description = "Forbidden"});
            }

            if (watchResponse == null || watchFaceRequest.Lat == null || watchFaceRequest.Lon == null ||
                watchResponse.WeatherInfo.RequestStatus.StatusCode == RequestStatusCode.Error ||
                watchResponse.LocationInfo.RequestStatus.StatusCode == RequestStatusCode.Error)
            {
                return BadRequest(new ErrorResponse {StatusCode = (int) HttpStatusCode.BadRequest, Description = "Bad request"});
            }

            return new 
            {
                watchResponse.WeatherInfo.WeatherProvider,
                watchResponse.WeatherInfo.Icon,
                watchResponse.WeatherInfo.PrecipProbability,
                watchResponse.WeatherInfo.Temperature,
                watchResponse.WeatherInfo.WindSpeed,
                watchResponse.WeatherInfo.Humidity,
                watchResponse.WeatherInfo.Pressure,
                watchResponse.LocationInfo.CityName,
                ServerVersion = SolutionInfo.Version
            };
        }

        /// <summary>
        /// Process request from the watchface and returns all requested data 
        /// </summary>
        /// <param name="watchRequest">watchface data</param>
        /// <returns>weather, location and exchange rate info</returns>
        [HttpGet, MapToApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField = "did", Seconds = 5)]
        [Authorize]
        public async Task<ActionResult<WatchResponse>> Get([FromQuery] WatchRequest watchRequest)
        {
            try
            {
                var weatherInfo = new WeatherInfo();
                var locationInfo = new LocationInfo();
                var exchangeRateInfo = new ExchangeRateInfo();

                if (watchRequest.Lat != null && watchRequest.Lon != null)
                {
                    // Get weather info
                    //
                    Enum.TryParse<WeatherProvider>(watchRequest.WeatherProvider, true, out var weatherProvider);
                    weatherInfo = (weatherProvider == WeatherProvider.DarkSky)
                        ? await _darkSkyClient.RequestDarkSky(watchRequest.Lat.Value, watchRequest.Lon.Value, watchRequest.DarkskyKey)
                        : await _openWeatherClient.RequestOpenWeather(watchRequest.Lat.Value, watchRequest.Lon.Value);

                    // Get location info
                    //
                    locationInfo =
                        await _postgresDataProvider.LoadLastLocation(watchRequest.DeviceId, watchRequest.Lat.Value, watchRequest.Lon.Value) ??
                        await _virtualearthClient.RequestLocationName(watchRequest.Lat.Value, watchRequest.Lon.Value);
                }

                // Get Exchange Rate info
                //
                if (!watchRequest.BaseCurrency.IsNullOrEmpty() && !watchRequest.TargetCurrency.IsNullOrEmpty())
                {
                    exchangeRateInfo = await _exchangeRateCacheStrategy
                        .GetExchangeRate(watchRequest.BaseCurrency, watchRequest.TargetCurrency);
                }

                // Save all requested data
                //
                await _postgresDataProvider.SaveRequestInfo(watchRequest, weatherInfo, locationInfo, exchangeRateInfo);
                locationInfo.CityName = locationInfo.CityName.StripDiacritics();

                var watchResponse = new WatchResponse
                {
                    LocationInfo = locationInfo,
                    WeatherInfo = weatherInfo,
                    ExchangeRateInfo = exchangeRateInfo
                };

                await _kafkaProvider.SendMessage(watchRequest);

                _logger.LogInformation(
                    new EventId(105, "WatchRequest"), "{@WatchRequest}, {@WatchResponse}", watchRequest, watchResponse);
                return watchResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Request error, {@WatchFaceRequest}", watchRequest);
                return BadRequest(new ErrorResponse {StatusCode = (int) HttpStatusCode.BadRequest, Description = "Bad request"});
            }
        }

    }
}