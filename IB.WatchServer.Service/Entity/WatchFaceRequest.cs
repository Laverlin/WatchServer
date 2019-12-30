using Microsoft.AspNetCore.Mvc;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Location request info
    /// </summary>
    public class WatchFaceRequest
    {
        /// <summary>
        /// Latitude
        /// </summary>
        [FromQuery(Name = "lat")]
        public string Lat { get; set; }

        /// <summary>
        /// Longitude
        /// </summary>
        [FromQuery(Name = "lon")]
        public string Lon { get; set; }

        /// <summary>
        /// Unique garmin device ID
        /// </summary>
        [FromQuery(Name = "did")]
        public string DeviceId { get; set; }

        /// <summary>
        /// application version
        /// </summary>
        [FromQuery(Name = "v")]
        public string Version { get; set; }

        /// <summary>
        /// Framework version
        /// </summary>
        [FromQuery(Name = "fw")]
        public string Framework { get; set; }

        /// <summary>
        /// CIQ version
        /// </summary>
        [FromQuery(Name = "ciqv")]
        public string CiqVersion { get; set; }

        /// <summary>
        /// Name of the device
        /// </summary>
        [FromQuery(Name = "dname")]
        public string DeviceName { get; set; }
    }
}
