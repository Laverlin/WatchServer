using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Security.Policy;

namespace IB.WatchServer.Service.Entity.Settings
{
    /// <summary>
    /// Configuration settings for the watchFace API
    /// </summary>
    public class FaceSettings
    {
        /// <summary>
        /// Location service url template
        /// </summary>
        [Required, Url]
        public string LocationUrl { get; set; }

        /// <summary>
        /// Location service api key
        /// </summary>
        [Required]
        [StringLength(maximumLength: 64, MinimumLength = 64)]
        public string LocationKey { get; set; }

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

        public ProxySettings ProxySettings { get; set; }

        [Required, Url]
        public string CurrencyConverterUrl { get; set; }

        [Required]
        public string CurrencyConverterKey { get; set; }

        [Required, Url]
        public string ExchangeRateApiUrl { get; set; }

        [Required, Url]
        public string ExchangeHostApiUrl { get; set; }

        [Required]
        public string[] ExchangeRateSupportedCurrency { get; set; }

        public bool Log2Kafka { get; set; } = true;

        public bool DisableYasBot {get; set; } = false;
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

    public class ProxySettings
    {
        public string Host { get; set; }

        public int Port { get; set; }
    }
}
