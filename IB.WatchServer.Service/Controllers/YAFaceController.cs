using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Infrastructure;

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

        public YAFaceController(
            ILogger<YAFaceController> logger, YAFaceProvider yaFaceProvider)
        {
            _logger = logger;
            _yaFaceProvider = yaFaceProvider;
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
        /// <param name="locationRequest"></param>
        /// <returns></returns>
        [HttpGet("location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[RequestRateLimit(Seconds = 5, KeyField = "did")]
        [RequestRateFactory(KeyField ="did", Seconds = 5)]
        public async Task<ActionResult<LocationResponse>> Location([FromQuery] LocationRequest locationRequest)
        {
            try
            {
                string city;
                string deviceId = locationRequest.DeviceId ?? "unknown";

                city = await _yaFaceProvider.RequestLocationName(locationRequest.Lat, locationRequest.Lon);

                var cityInfo = new CityInfo
                {
                    CityName = city,
                    Lat = Convert.ToDecimal(locationRequest.Lat),
                    Lon = Convert.ToDecimal(locationRequest.Lon),
                    RequestTime = DateTime.UtcNow,
                    Version = locationRequest.Version,
                    Framework = locationRequest.Framework,
                    CiqVersion = locationRequest.CiqVersion
                };

                await _yaFaceProvider.SaveRequest(deviceId, locationRequest.DeviceName, cityInfo);

                var locationResponse = new LocationResponse { CityName = _yaFaceProvider.RemoveDiacritics(city) };

                _logger.LogInformation(new EventId(100, "LocationRequest"), "CityName {CityName}, LocationRequest {@LocationRequest}", locationResponse.CityName, locationRequest);

                return locationResponse;

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Process location request error");
                return BadRequest();
            }

        }

    }
}