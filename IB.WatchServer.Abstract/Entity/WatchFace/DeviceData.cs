using System;
using LinqToDB.Mapping;

namespace IB.WatchServer.Abstract.Entity.WatchFace
{
    /// <summary>
    /// Information about the device
    /// </summary>
    [Table(Name = "DeviceInfo")]
    public class DeviceData
    {
        /// <summary>
        /// Unique internal id
        /// </summary>
        [Column(Name = "id"), Identity, PrimaryKey]
        public int? Id { get; set; }

        /// <summary>
        /// Garmin device id
        /// </summary>
        [Column(Name = "DeviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Device name
        /// </summary>
        [Column(Name = "DeviceName")]
        public string DeviceName { get; set; }

        /// <summary>
        /// Time of the first request from the device
        /// </summary>
        [Column(Name = "FirstRequestTime")]
        public DateTime FirstRequestTime { get; set; }
    }
}
