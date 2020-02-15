using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Configuration settings for the watchFace API
    /// </summary>
    public class FaceSettings
    {
        /// <summary>
        /// Service url template
        /// </summary>
        [Required, Url]
        public string BaseUrl { get; set; }

        /// <summary>
        /// Service api key
        /// </summary>
        [Required]
        [StringLength(maximumLength: 64, MinimumLength = 64)]
        public string ApiKey { get; set; }

        /// <summary>
        /// Url Template for the weather service request 
        /// </summary>
        [Required, Url]
        public string DarkSkyUrl { get; set; }

        /// <summary>
        /// Authentication key for weather api
        /// </summary>
        [Required]
        public string DarkSkyKey { get; set; }

        /// <summary>
        /// Authentication settings of the application
        /// </summary>
        [Required]
        public AuthSettings AuthSettings { get; set; }

        [Required, Url]
        public string OpenWeatherUrl { get; set; }

        [Required]
        public string OpenWeatherKey { get; set; }
       
        [Required]
        public string TelegramKey { get; set; }

    }

    /// <summary>
    /// Authentication settings
    /// </summary>
    public class AuthSettings
    {
        /// <summary>
        /// Name of the authentication scheme
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Name of the query parameter of token
        /// </summary>
        public string TokenName { get; set; } 

        /// <summary>
        /// Token value
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// Helper class to get data from configuration object
    /// </summary>
    public static class FaceSettingsExtensions
    {
        /// <summary>
        /// Build url string to request location info
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="lat">Latitude of location</param>
        /// <param name="lon">Longitude of location</param>
        /// <returns>url with parameters to get location name</returns>
        public static Uri BuildLocationUrl(this FaceSettings settings, string lat, string lon)
        {
            return new Uri(string.Format(settings.BaseUrl, lat, lon, settings.ApiKey));
        }

        /// <summary>
        /// Build url string to request DarkSky service
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="lat">Location latitude</param>
        /// <param name="lon">Location longitude</param>
        /// <param name="dsToken">DarkSky authentication token</param>
        /// <returns>Url to request the weather from DarkSky</returns>
        public static Uri BuildDarkSkyUrl(this FaceSettings settings, string lat, string lon, string dsToken)
        {
            return new Uri(string.Format(settings.DarkSkyUrl, dsToken, lat, lon));
        }

        /// <summary>
        /// Build url string to request the weather from OpenWeather
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="lat">Latitude of location</param>
        /// <param name="lon">Longitude of location</param>
        /// <returns>url with parameters to get the weather from OpenWeather</returns>
        public static Uri BuildOpenWeatherUrl(this FaceSettings settings, string lat, string lon)
        {
            return new Uri(string.Format(settings.OpenWeatherUrl, lat, lon, settings.OpenWeatherKey));
        }

    }
}
