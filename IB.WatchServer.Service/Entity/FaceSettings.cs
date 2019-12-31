using System.ComponentModel.DataAnnotations;


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
        public string WeatherBaseUrl { get; set; }

        /// <summary>
        /// Authentication key for weather api
        /// </summary>
        [Required]
        public string WeatherApiKey { get; set; }

        /// <summary>
        /// Authentication settings of the application
        /// </summary>
        [Required]
        public AuthSettings AuthSettings { get; set; }
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
}
