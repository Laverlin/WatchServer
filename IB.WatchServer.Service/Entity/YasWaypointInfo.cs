using LinqToDB.Mapping;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Way point data
    /// </summary>
    [Table("yas_waypoint")]
    public class YasWaypointInfo
    {
        [Column("waypoint_id", IsIdentity = true)]
        public long WaypointId {get;set;}

        [Column("route_id")]
        public long RouteId {get;set;}

        [Column("waypoint_name")]
        public string Name {get;set;}

        [Column("lat")]
        public decimal Latitude {get;set;}

        [Column("lon")]
        public decimal Longitude {get;set;}

        [Column("order_id")]
        public int OrderId {get;set;}
    }
}
