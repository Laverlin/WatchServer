using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.V1
{
    /// <summary>
    /// Response with the current weather condition and city name 
    /// </summary>
    public class WeatherResponse : BaseApiResponse
    {
        /// <summary>
        /// The weather provider that actually processed the request
        /// </summary>
        [JsonPropertyName("weatherProvider")]
        public string WeatherProvider { get; set; }

        /// <summary>
        /// Icon id
        /// </summary>
        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        /// <summary>
        /// Precipitation probability if request was processed by DarkSky
        /// </summary>
        [JsonPropertyName("precipProbability")]
        public decimal PrecipProbability { get; set; }

        /// <summary>
        /// Current Temperature in Celsius
        /// </summary>
        [JsonPropertyName("temperature")]
        public decimal Temperature { get; set; }

        /// <summary>
        /// Current Wind speed in m/s
        /// </summary>
        [JsonPropertyName("windSpeed")]
        public decimal WindSpeed { get; set; }

        /// <summary>
        /// Current humidity
        /// </summary>
        [JsonPropertyName("humidity")]
        public decimal Humidity { get; set; }

        /// <summary>
        /// Current Atmospheric Pressure in mm 
        /// </summary>
        [JsonPropertyName("pressure")]
        public decimal Pressure { get; set; }

        /// <summary>
        /// Location name
        /// </summary>
        public string CityName { get; set; }

    }
}