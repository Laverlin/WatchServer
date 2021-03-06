﻿using LinqToDB.Mapping;
using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.SailingApp
{
    /// <summary>
    /// Way point data
    /// </summary>
    [Table("yas_waypoint")]
    public class YasWaypoint
    {
        [Column("waypoint_id", IsIdentity = true)]
        public long WaypointId {get;set;}

        [Column("route_id")]
        public long RouteId {get;set;}

        [Column("waypoint_name")]
        public string Name {get;set;}

        [Column("lat")]
        [JsonPropertyName("Lat")]
        public decimal Latitude {get;set;}

        [Column("lon")]
        [JsonPropertyName("Lon")]
        public decimal Longitude {get;set;}

        [Column("order_id")]
        [JsonIgnore]
        public int OrderId {get;set;}
    }
}
