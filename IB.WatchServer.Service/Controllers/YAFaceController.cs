using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Service;

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

        /*

        [HttpGet("location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
      //  [RequestRateLimit(Seconds = 5, KeyField = "did")]
        public async Task<ActionResult<LocationInfo>> Location([FromQuery] WatchRequest watchRequest)
        {
            try
            {
                string city;
                string deviceId = watchRequest.DeviceId ?? "unknown";

                city = await _locationService.RequestLocationName(watchRequest.Lat, watchRequest.Lon);

                var cityInfo = new CityInfo
                {
                    CityName = city,
                    Lat = Convert.ToDecimal(watchRequest.Lat),
                    Lon = Convert.ToDecimal(watchRequest.Lon),
                    RequestTime = DateTime.UtcNow,
                    Version = watchRequest.Version,
                    Framework = watchRequest.Framework,
                    CiqVersion = watchRequest.CiqVersion
                };

                await _locationService.SaveRequest(deviceId, watchRequest.DeviceName, cityInfo);

                var locationInfo = new LocationInfo { CityName = _locationService.RemoveDiacritics(city) };

                _logger.LogInformation(new EventId(100, "WatchRequestLog"), "CityName {CityName}, WatchRequest {@WatchRequest}", locationInfo.CityName, watchRequest);

                return locationInfo;

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Process location request error");
                return BadRequest();
            }

        }
        */
    }
}