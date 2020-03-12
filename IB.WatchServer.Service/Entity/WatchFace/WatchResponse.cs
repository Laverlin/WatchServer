using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    public class WatchResponse : BaseApiResponse
    {
        [JsonPropertyName("location")]
        public LocationInfo LocationInfo { get; set; }

        [JsonPropertyName("weather")]
        public WeatherInfo WeatherInfo { get; set; }
    }
}
