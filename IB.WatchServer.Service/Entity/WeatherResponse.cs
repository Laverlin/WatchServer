using System.Text.Json.Serialization;
using AutoMapper.Configuration.Annotations;
using IB.WatchServer.Service.Infrastructure;

namespace IB.WatchServer.Service.Entity
{
    public class WeatherResponse
    {
        [JsonPropertyName("apiVersion")]
        public string ApiVersion => SolutionInfo.GetVersion();

        [JsonPropertyName("weatherProvider")]
        public string WeatherProvider { get; set; }

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

        public string CityName { get; set; }
    }
}