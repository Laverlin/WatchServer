using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using App.Metrics;
using App.Metrics.Counter;
using AutoMapper;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using LinqToDB.Tools;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service
{
    public class WebRequestsProvider
    {
        private readonly ILogger<WebRequestsProvider> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;
        private readonly IMapper _mapper;
        private static readonly MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

        public WebRequestsProvider(
            ILogger<WebRequestsProvider> logger, IHttpClientFactory clientFactory, FaceSettings faceSettings, IMetrics metrics, IMapper mapper)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _faceSettings = faceSettings;
            _metrics = metrics;
            _mapper = mapper;
        }

        /// <summary>
        /// Request weather info on DarkSky weather provider
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="token">ApiToken</param>
        /// <returns>Weather info <see cref="RequestDarkSky"/></returns>
        public async Task<WeatherInfo> RequestDarkSky(string lat, string lon, string token)
        {
            string providerName = WeatherProvider.DarkSky.ToString();
            _metrics.Measure.Counter.Increment(new CounterOptions {Name = "weatherRequest", MeasurementUnit = Unit.Calls}, providerName);

            var client = _clientFactory.CreateClient();
            using var response = await client.GetAsync(_faceSettings.BuildDarkSkyUrl(lat, lon, token));
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(response.StatusCode == HttpStatusCode.Unauthorized
                    ? $"Unauthorized access to {providerName}"
                    : $"Error {providerName} request, status: {response.StatusCode.ToString()}");
                return new WeatherInfo {IsError = true, HttpStatusCode = (int)response.StatusCode};
            }

            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);
            var weatherInfo = JsonSerializer.Deserialize<WeatherInfo>(
                json.RootElement.GetProperty("currently").GetRawText());
            weatherInfo.WeatherProvider = providerName;
            weatherInfo.HttpStatusCode = (int) response.StatusCode;

            return weatherInfo;
        }

        /// <summary>
        /// Request weather conditions from OpenWeather service
        /// </summary>
        /// <param name="lat">latitude</param>
        /// <param name="lon">longitude</param>
        /// <returns>Weather conditions for the specified coordinates <see cref="WeatherResponse"/></returns>
        public async Task<WeatherInfo> RequestOpenWeather(string lat, string lon)
        {
            var providerName = WeatherProvider.OpenWeather.ToString();
            _metrics.Measure.Counter.Increment(new CounterOptions {Name = "weatherRequest", MeasurementUnit = Unit.Calls}, providerName);

            var conditionIcons = new Dictionary<string, string>
            {
                {"01d", "clear-day"}, {"01n", "clear-night"}, 
                {"10d", "rain"}, {"10n", "rain"}, {"09d", "rain"}, {"09n", "rain"},  {"11d", "rain"}, {"11n", "rain"},
                {"13d", "snow"}, {"13n", "snow"},  
                {"50d", "fog"}, {"50n", "fog"},
                {"03d","cloudy"}, {"03n","cloudy"}, 
                {"02d", "partly-cloudy-day"}, {"02n", "partly-cloudy-night"}, {"04d", "partly-cloudy-day"}, {"04n", "partly-cloudy-night"}
            };

            var client = _clientFactory.CreateClient();
            using var response = await client.GetAsync(_faceSettings.BuildOpenWeatherUrl(lat, lon));
            if (!response.IsSuccessStatusCode)
            { 
                _logger.LogWarning(response.StatusCode == HttpStatusCode.Unauthorized
                    ? $"Unauthorized access to {providerName}"
                    : $"Error {providerName} request, status: {response.StatusCode.ToString()}");
                return new WeatherInfo {IsError = true, HttpStatusCode = (int)response.StatusCode};
            }

            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);

            var elements = json.RootElement.EnumerateObject()
                .Where(e => e.Name.In("main", "weather", "wind"))
                .SelectMany(e => (e.Value.ValueKind == JsonValueKind.Array ? e.Value[0] : e.Value).EnumerateObject())
                .Where(e => e.Name.In("temp", "humidity", "pressure", "speed", "icon"))
                .ToDictionary(e => e.Name, v => v.Name == "icon" 
                    ? (object) (conditionIcons.ContainsKey(v.Value.GetString()) ? conditionIcons[v.Value.GetString()] : "clear-day") 
                    : v.Value.GetDecimal());

            var weatherInfo = _mapper.Map<WeatherInfo>(_mapper.Map<WeatherResponse>(elements));
            weatherInfo.WeatherProvider = providerName;
            weatherInfo.HttpStatusCode = (int) response.StatusCode;

            return weatherInfo;
        }

        /// <summary>
        /// Request current Exchange rate on  currencyconverterapi.com
        /// </summary>
        /// <param name="baseCurrency">the currency from which convert</param>
        /// <param name="targetCurrency">the currency to which convert</param>
        /// <returns>exchange rate. if conversion is unsuccessfull the rate could be 0 </returns>
        public async Task<ExchangeRateInfo> RequestExchangeRate(string baseCurrency, string targetCurrency)
        {
            var client = _clientFactory.CreateClient();
            using var response = await client.GetAsync(_faceSettings.BuildCurrencyConverterUrl(baseCurrency, targetCurrency));
            if (!response.IsSuccessStatusCode)
            { 
                _logger.LogWarning(response.StatusCode == HttpStatusCode.Unauthorized
                    ? $"Unauthorized access to currencyconverterapi.com"
                    : $"Error currencyconverterapi.com request, status: {response.StatusCode.ToString()}");
                return new ExchangeRateInfo {IsError = true, HttpStatusCode = (int)response.StatusCode};
            }

            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);

            return new ExchangeRateInfo
            {
                ExchangeRate = json.RootElement.TryGetProperty($"{baseCurrency}_{targetCurrency}", out var rate) 
                    ? rate.GetDecimal() : 0,
                HttpStatusCode = (int)response.StatusCode
            };
        }

        /// <summary>
        /// Request current Exchange rate in local cache
        /// </summary>
        /// <param name="baseCurrency">the currency from which convert</param>
        /// <param name="targetCurrency">the currency to which convert</param>
        /// <param name="exchangeRateFunc">function to get exchange rate</param>
        /// <returns>Returns exchange rate, if cache is missed returns null</returns>
        public async Task<ExchangeRateInfo> RequestCacheExchangeRate(
            string baseCurrency, string targetCurrency, Func<string, string, Task<ExchangeRateInfo>> exchangeRateFunc)
        {
            string cacheKey = $"er-{baseCurrency}-{targetCurrency}";
            if (_memoryCache.TryGetValue(cacheKey, out ExchangeRateInfo exchangeRateInfo))
            {
                _metrics.Measure.Counter.Increment(new CounterOptions{Name = "exchangeRate-cache"}, $"{baseCurrency}-{targetCurrency}");
                return exchangeRateInfo;
            }

            _metrics.Measure.Counter.Increment(new CounterOptions{Name = "exchangeRate-request"}, $"{baseCurrency}-{targetCurrency}");
            exchangeRateInfo = await exchangeRateFunc(baseCurrency, targetCurrency);
            if (!exchangeRateInfo.IsError && exchangeRateInfo.ExchangeRate != 0)
                _memoryCache.Set(cacheKey, exchangeRateInfo, TimeSpan.FromMinutes(60));
            return exchangeRateInfo;
        }
    }
}
