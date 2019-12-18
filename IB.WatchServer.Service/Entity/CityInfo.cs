using System;
using LinqToDB.Mapping;

namespace IB.WatchServer.Service.Entity
{
    [Table("CityInfo")]
    public class CityInfo
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
    }
}
