using System.Text.Json.Serialization;
using AutoMapper.Configuration.Annotations;

namespace IB.WatchServer.Service.Entity
{
    public class WeatherResponse
    {
        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("precipProbability")]
        public decimal PrecipProbability { get; set; }

        [JsonPropertyName("temperature")]
        public decimal Temperature { get; set; }

        [JsonPropertyName("windSpeed")]
        public decimal WindSpeed { get; set; }

        [JsonPropertyName("humidity")]
        public decimal Humidity { get; set; }

        [JsonPropertyName("pressure")]
        public decimal Pressure { get; set; }

        public string CityName { get; internal set; }
    }
}