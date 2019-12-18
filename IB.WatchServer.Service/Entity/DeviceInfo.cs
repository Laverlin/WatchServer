using System;
using LinqToDB.Mapping;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Information about the device
    /// </summary>
    [Table(Name = "DeviceInfo")]
    public class DeviceInfo
    {
        /// <summary>
        /// Unique id
        /// </summary>
        [Column(Name = "id"), Identity, PrimaryKey]
        public int? Id { get; set; }

        /// <summary>
        /// garmmin device unique id
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
