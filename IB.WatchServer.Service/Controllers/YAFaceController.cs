using System;
using System.Net;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Entity.WatchFace;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Infrastructure;
using AutoMapper;

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
        private readonly YAFaceProvider _yaFaceProvider;
        private readonly DataProvider _dataProvider;
        private readonly WebRequestsProvider _webRequestsProvider;
        private readonly IMetrics _metrics;
        private readonly IMapper _mapper;

        public YAFaceController(
            ILogger<YAFaceController> logger, YAFaceProvider yaFaceProvider, 
            DataProvider dataProvider, WebRequestsProvider webRequestsProvider, IMetrics metrics, IMapper mapper)
        {
            _logger = logger;
            _yaFaceProvider = yaFaceProvider;
            _dataProvider = dataProvider;
            _webRequestsProvider = webRequestsProvider;
            _metrics = metrics;
            _mapper = mapper;
        }

        /// <summary>
        /// Provide the response for the health check request
        /// </summary>
        /// <returns><see cref="Pong"/>The number of registered devices</returns>
        [HttpGet("Ping")]
        [MapToApiVersion("1.0")]
        public async Task<Pong> Ping(ApiVersion apiVersion)
        {
            var deviceCount = await _yaFaceProvider.GetDeviceCount();
            return new Pong
            {
                DeviceCount = deviceCount,
                ApiVersion = apiVersion.ToString()
            };
        }


        /// <summary>
        /// Process request of the location
        /// </summary>
        /// <param name="watchFaceRequest"></param>
        /// <returns></returns>
        [HttpGet("location"), MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField ="did", Seconds = 5)]
        public async Task<ActionResult<LocationResponse>> Location([FromQuery] WatchFaceRequest watchFaceRequest)
        {
            try
            {
                var city = await GetLocationName(watchFaceRequest, RequestType.Location);
                await _yaFaceProvider.SaveRequestInfo(watchFaceRequest, city);
                var locationResponse = new LocationResponse { CityName = city.StripDiacritics() };

                _logger.LogInformation(
                    new EventId(100, "LocationRequest"),
                    "{@WatchFaceRequest}, {@LocationResponse}", watchFaceRequest, locationResponse);

                return locationResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Location request error, {@WatchFaceRequest}", watchFaceRequest);
                return BadRequest();
            }
        }

        /// <summary>
        /// Provide weather info
        /// </summary>
        /// <param name="watchFaceRequest"></param>
        /// <returns>The <see cref="WeatherResponse"/> data of current weather in given location</returns>
        [HttpGet("weather"), MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField = "did", Seconds = 5)]
        [Authorize]
        public async Task<ActionResult<WeatherResponse>> Weather([FromQuery] WatchFaceRequest watchFaceRequest)
        {
            try
            {
                Enum.TryParse<WeatherProvider>(watchFaceRequest.WeatherProvider, true, out var weatherProvider);
                var weatherResponse = (weatherProvider == WeatherProvider.DarkSky || watchFaceRequest.DarkskyKey?.Length == 32)
                    ? await _yaFaceProvider.RequestDarkSky(watchFaceRequest.Lat, watchFaceRequest.Lon, watchFaceRequest.DarkskyKey)
                    : await _yaFaceProvider.RequestOpenWeather(watchFaceRequest.Lat, watchFaceRequest.Lon);

                weatherResponse.CityName = await GetLocationName(watchFaceRequest, RequestType.Weather);
                await _yaFaceProvider.SaveRequestInfo(RequestType.Weather, watchFaceRequest, weatherResponse);
                weatherResponse.CityName = weatherResponse.CityName.StripDiacritics();

                _logger.LogInformation(
                    new EventId(101, "WeatherRequest"),
                    "{@WatchFaceRequest}, {@WeatherResponse}", watchFaceRequest, weatherResponse);
                return weatherResponse;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized weather request: {@WatchFaceRequest}", watchFaceRequest);
                return StatusCode((int) HttpStatusCode.Forbidden,
                    new ErrorResponse {StatusCode = (int) HttpStatusCode.Forbidden, Description = "Forbidden"});
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Weather request error, {@WatchFaceRequest}", watchFaceRequest);
                return BadRequest(new ErrorResponse {StatusCode = (int) HttpStatusCode.BadRequest, Description = "Bad request"});
            }
        }


        [HttpGet, MapToApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField = "did", Seconds = 5)]
        [Authorize]
        public async Task<ActionResult<WatchResponse>> Get([FromQuery] WatchFaceRequest watchFaceRequest)
        {
            try
            {
                Enum.TryParse<WeatherProvider>(watchFaceRequest.WeatherProvider, true, out var weatherProvider);
                var weatherInfo = (weatherProvider == WeatherProvider.DarkSky)
                    ? await _webRequestsProvider.RequestDarkSky(watchFaceRequest.Lat, watchFaceRequest.Lon, watchFaceRequest.DarkskyKey)
                    : await _webRequestsProvider.RequestOpenWeather(watchFaceRequest.Lat, watchFaceRequest.Lon);

                var locationInfo = new LocationInfo
                {
                    CityName = await _yaFaceProvider.CheckLastLocation(watchFaceRequest.DeviceId,
                                   Convert.ToDecimal(watchFaceRequest.Lat), Convert.ToDecimal(watchFaceRequest.Lon))
                               ?? await _yaFaceProvider.RequestLocationName(watchFaceRequest.Lat, watchFaceRequest.Lon)
                };

                await _dataProvider.SaveRequestInfo(watchFaceRequest, weatherInfo, locationInfo);
                locationInfo.CityName = locationInfo.CityName.StripDiacritics();

                var watchResponse = new WatchResponse
                {
                    LocationInfo = locationInfo,
                    WeatherInfo = weatherInfo
                };
                _logger.LogInformation(
                    new EventId(105, "WatchRequest"), "{@WatchRequest}, {@WatchResponse}", watchFaceRequest, watchResponse);
                return watchResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Request error, {@WatchFaceRequest}", watchFaceRequest);
                return BadRequest(new ErrorResponse {StatusCode = (int) HttpStatusCode.BadRequest, Description = "Bad request"});
            }
        }



        private async Task<string> GetLocationName(WatchFaceRequest watchFaceRequest, RequestType requestType)
        {
            _metrics.Measure.Counter.Increment(
                new CounterOptions {Name = "locationRequest-total", MeasurementUnit = Unit.Calls}, 
                requestType.ToString());
            var cityName = await _yaFaceProvider.CheckLastLocation(
                watchFaceRequest.DeviceId, Convert.ToDecimal(watchFaceRequest.Lat), Convert.ToDecimal(watchFaceRequest.Lon));

            if (cityName == null)
            {
                _metrics.Measure.Counter.Increment(
                    new CounterOptions {Name = "locationRequest-remote", MeasurementUnit = Unit.Calls},
                    requestType.ToString());
                cityName = await _yaFaceProvider.RequestLocationName(watchFaceRequest.Lat, watchFaceRequest.Lon);
            }

            return cityName;
        }
    }
}