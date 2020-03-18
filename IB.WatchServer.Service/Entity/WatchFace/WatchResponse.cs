using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    /// <summary>
    /// Response data to the watch request
    /// </summary>
    public class WatchResponse : BaseApiResponse
    {
        /// <summary>
        /// Location name in given coordinates
        /// </summary>
        [JsonPropertyName("location")]
        public LocationInfo LocationInfo { get; set; } = new LocationInfo();

        /// <summary>
        /// Weather data in given coordinates
        /// </summary>
        [JsonPropertyName("weather")]
        public WeatherInfo WeatherInfo { get; set; } = new WeatherInfo();

        /// <summary>
        /// Exchange rate between two given currencies 
        /// </summary>
        [JsonPropertyName("exchange")]
        public ExchangeRateInfo ExchangeRateInfo { get; set; } = new ExchangeRateInfo();
    }
}
