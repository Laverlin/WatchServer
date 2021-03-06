﻿using System.Text.Json.Serialization;

namespace IB.WatchServer.Abstract.Entity.WatchFace
{
    /// <summary>
    /// Weather data
    /// </summary>
    public class WeatherInfo 
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
        /// Status of request to the remote server
        /// </summary>
        [JsonPropertyName("status")]
        public RequestStatus RequestStatus { get; set; } = new RequestStatus();
    }
}