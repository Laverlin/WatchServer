using System;
using LinqToDB.Mapping;

namespace IB.WatchServer.Service.Entity
{
    [Table("CityInfo")]
    public class RequestInfo
    {
        [Column("id"), Identity]
        public int? Id { get; set; }

        [Column("DeviceInfoId")]
        public int? DeviceInfoId { get; set; }

        [Column("RequestTime")]
        public DateTime RequestTime { get; set; }

        [Column("CityName")]
        public string CityName { get; set; }

        [Column("Lat")]
        public decimal Lat { get; set; }

        [Column("Lon")]
        public decimal Lon { get; set; }

        [Column("FaceVersion")]
        public string Version { get; set; }

        [Column("FrameworkVersion")]
        public string Framework { get; set; }

        [Column("CIQVersion")]
        public string CiqVersion { get; set; }

        [Column("RequestType")]
        public RequestType RequestType { get; set; }

        [Column("Temperature")]
        public decimal Temperature { get; set; }

        [Column("Wind")]
        public decimal Wind { get; set; }

        [Column("PrecipProbability")]
        public decimal PrecipProbability { get; set; }
    }

    public enum RequestType
    {
        [MapValue("Location")]
        Location = 0,

        [MapValue("Weather")]
        Weather = 1,

        [MapValue("ExchangeRate")]
        ExchangeRate = 2
    }
}
