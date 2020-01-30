using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Route Information
    /// </summary>
    [Table("yas_route_info")]
    public class YasRouteInfo
    {
        [Column("route_id")]
        public long RouteId { get; set; } 

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("route_name")]
        public string RouteName { get; set; }

        [Column("upload_time")]
        public DateTime UploadTime { get; set; }
    }
}
