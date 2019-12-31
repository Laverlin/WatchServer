using System;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace IB.WatchServer.Service.Controllers
{
    /// <summary>
    /// Controller for the watch face requests
    /// </summary>
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class YAFaceController : ControllerBase
    {
        private readonly ILogger<YAFaceController> _logger;
        private readonly YAFaceProvider _yaFaceProvider;
        private readonly IMetrics _metrics;

        public YAFaceController(
            ILogger<YAFaceController> logger, YAFaceProvider yaFaceProvider, IMetrics metrics)
        {
            _logger = logger;
            _yaFaceProvider = yaFaceProvider;
            _metrics = metrics;
        }

        /// <summary>
        /// Provide the response for the health check request
        /// </summary>
        /// <returns><see cref="Pong"/>The number of registered devices</returns>
        [HttpGet("Ping")]
        public async Task<Pong> Ping()
        {
            var deviceCount = await _yaFaceProvider.GetDeviceCount();
            return new Pong { DeviceCount = deviceCount };
        }


        /// <summary>
        /// Process request of the location
        /// </summary>
        /// <param name="watchFaceRequest"></param>
        /// <returns></returns>
        [HttpGet("location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField ="did", Seconds = 5)]
        public async Task<ActionResult<LocationResponse>> Location([FromQuery] WatchFaceRequest watchFaceRequest)
        {
            try
            {
                var city = await GetLocationName(watchFaceRequest, RequestType.Location);
                await _yaFaceProvider.SaveRequestInfo(watchFaceRequest, city);
                var locationResponse = new LocationResponse { CityName = _yaFaceProvider.RemoveDiacritics(city) };

                _logger.LogInformation(
                    new EventId(100, "LocationRequest"),
                    "LocationRequest {@WatchFaceRequest}, CityName {CityName}", watchFaceRequest, locationResponse.CityName);

                return locationResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Location request error");
                return BadRequest();
            }
        }

        /// <summary>
        /// Provide weather info
        /// </summary>
        /// <param name="watchFaceRequest"></param>
        /// <returns></returns>
        [HttpGet("weather")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestRateFactory(KeyField = "did", Seconds = 5)]
        [Authorize]
        public async Task<ActionResult<WeatherResponse>> Weather([FromQuery] WatchFaceRequest watchFaceRequest)
        {
            try
            {
                var weatherResponse = await _yaFaceProvider.RequestWeather(watchFaceRequest.Lat, watchFaceRequest.Lon);
                weatherResponse.CityName = await GetLocationName(watchFaceRequest, RequestType.Weather);
                
                await _yaFaceProvider.SaveRequestInfo(RequestType.Weather, watchFaceRequest, weatherResponse);
                _logger.LogInformation(
                    new EventId(101, "WeatherRequest"),
                    "WatchFaceRequest {@WatchFaceRequest}, WeatherResponse {@WeatherResponse}", watchFaceRequest, weatherResponse);

                return weatherResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Weather request error");
                return BadRequest();
            }
        }

        private async Task<string> GetLocationName(WatchFaceRequest watchFaceRequest, RequestType requestType)
        {
            var locationCounterTotal = new CounterOptions { Name = "locationRequest-total" };
            var locationCounterRemote = new CounterOptions { Name = "locationRequest-remote" };

            _metrics.Measure.Counter.Increment(locationCounterTotal, requestType.ToString());
            var cityName = await _yaFaceProvider.CheckLastLocation(
                watchFaceRequest.DeviceId, Convert.ToDecimal(watchFaceRequest.Lat), Convert.ToDecimal(watchFaceRequest.Lon));
            if (cityName == null)
            {
                _metrics.Measure.Counter.Increment(locationCounterRemote, requestType.ToString());
                cityName = await _yaFaceProvider.RequestLocationName(watchFaceRequest.Lat, watchFaceRequest.Lon);
            }
            return cityName;
        }
    }
}