using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace IB.WatchServer.Service.Entity.Settings
{
    /// <summary>
    /// Helper class to get data from configuration object
    /// </summary>
    public static class SettingsExtensions
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
            return new Uri(string.Format(settings.LocationUrl, lat, lon, settings.LocationKey));
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

        /// <summary>
        /// Build url string to request the weather from OpenWeather
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="baseCurrency">base currency for exchange rate</param>
        /// <param name="targetCurrency">target currency for exchange rate</param>
        /// <returns>url with parameters to get exchange rate from free.currencyconverter.com</returns>
        public static Uri BuildCurrencyConverterUrl(this FaceSettings settings, string baseCurrency, string targetCurrency)
        {
            return new Uri(string.Format(settings.CurrencyConverterUrl, settings.CurrencyConverterKey, baseCurrency, targetCurrency));
        }

        /// <summary>
        /// Build connection string by combining all public properties in string in format "name=value;..."
        /// If property name needs to be different you need to use the DisplayName Attribute
        /// </summary>
        /// <returns>Connection String</returns>
        public static string BuildConnectionString(this IConnectionSettings connectionSettings)
        {
            var connectionString = new StringBuilder();
            foreach (var propertyInfo in connectionSettings.GetType().GetProperties())
            {
                var value = propertyInfo.GetValue(connectionSettings);
                if (value != null)
                {
                    var name = propertyInfo.GetCustomAttribute<DisplayNameAttribute>() != null
                        ? propertyInfo.GetCustomAttribute<DisplayNameAttribute>().DisplayName
                        : propertyInfo.Name;
                    connectionString.Append($"{name}={value};");
                }
            }

            return connectionString.ToString();
        }
    }
}
