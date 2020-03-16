using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    public class WatchResponse : BaseApiResponse
    {
        [JsonPropertyName("location")]
        public LocationInfo LocationInfo { get; set; } = new LocationInfo();

        [JsonPropertyName("weather")]
        public WeatherInfo WeatherInfo { get; set; } = new WeatherInfo();

        [JsonPropertyName("exchange")]
        public ExchangeRateInfo ExchangeRateInfo { get; set; } = new ExchangeRateInfo();
    }
}
