using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    public class LocationInfo : BaseResponseInfo
    {
        [JsonPropertyName("cityName")]
        public string CityName { get; set;}
    }
}