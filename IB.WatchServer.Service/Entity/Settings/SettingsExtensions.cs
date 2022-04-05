using System;

namespace IB.WatchServer.Service.Entity.Settings
{
    public static class SettingsExtensions
    {
                /// <summary>
        /// Build url string to request location info
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="lat">Latitude of location</param>
        /// <param name="lon">Longitude of location</param>
        /// <returns>url with parameters to get location name</returns>
        public static Uri BuildLocationUrl(this FaceSettings settings, decimal lat, decimal lon)
        {
            return new Uri(string.Format(settings.LocationUrl, lat.ToString("G"), lon.ToString("G"), settings.LocationKey));
        }

        /// <summary>
        /// Build url string to request DarkSky service
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="lat">Location latitude</param>
        /// <param name="lon">Location longitude</param>
        /// <param name="dsToken">DarkSky authentication token</param>
        /// <returns>Url to request the weather from DarkSky</returns>
        public static Uri BuildDarkSkyUrl(this FaceSettings settings, decimal lat, decimal lon, string dsToken)
        {
            return new Uri(string.Format(settings.DarkSkyUrl, dsToken, lat.ToString("G"), lon.ToString("G")));
        }

        /// <summary>
        /// Build url string to request the weather from OpenWeather
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="lat">Latitude of location</param>
        /// <param name="lon">Longitude of location</param>
        /// <returns>url with parameters to get the weather from OpenWeather</returns>
        public static Uri BuildOpenWeatherUrl(this FaceSettings settings, decimal lat, decimal lon)
        {
            return new Uri(string.Format(settings.OpenWeatherUrl, lat, lon, settings.OpenWeatherKey));
        }

        /// <summary>
        /// Build url string to request the currency exchange from CurrencyConverter service
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
        /// Build url string to request the exchange rate from ExchangeRateApi.com
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="baseCurrency">base currency for exchange rate</param>
        /// <param name="targetCurrency">target currency for exchange rate</param>
        /// <returns>url with parameters to get exchange rate from from ExchangeRateApi.com</returns>
        public static Uri BuildExchangeRateApiUrl(this FaceSettings settings, string baseCurrency, string targetCurrency)
        {
            return new Uri(string.Format(settings.ExchangeRateApiUrl, baseCurrency, targetCurrency));
        }

        /// <summary>
        /// Build url string to request the exchange rate from api.exchangerate.host
        /// </summary>
        /// <param name="settings">Configuration object</param>
        /// <param name="baseCurrency">base currency for exchange rate</param>
        /// <param name="targetCurrency">target currency for exchange rate</param>
        /// <returns>url with parameters to get exchange rate from from api.exchangerate.host</returns>
        public static Uri BuildExchangeHostApiUrl(this FaceSettings settings, string baseCurrency, string targetCurrency)
        {
            return new Uri(string.Format(settings.ExchangeHostApiUrl, baseCurrency, targetCurrency));
        }
    }
}