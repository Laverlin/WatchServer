using System;
using System.Linq;
using System.Text.Json.Serialization;
using LinqToDB.Mapping;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Route Information
    /// </summary>
    [Table("yas_route_info")]
    public class YasRouteInfo
    {
        [Column("route_id", IsIdentity = true)]
        public long RouteId { get; set; } 

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("route_name")]
        [JsonPropertyName("RouteName")]
        public string RouteName { get; set; }

        [Column("upload_time")]
        [JsonPropertyName("RouteDate")]
        public DateTime UploadTime { get; set; }

        [JsonPropertyName("WayPoints")]
        public IOrderedEnumerable<YasWaypointInfo> Waypoints { get; set; }
    }
}
