using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using App.Metrics;
using AutoMapper;

using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using LinqToDB.Tools;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace IB.WatchServer.Service.Service
{
    public class WebRequestsProvider
    {
        private readonly ILogger<WebRequestsProvider> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;
        private readonly IMapper _mapper;
        private readonly CurrencyConverterClient _currencyConverterClient;
        private readonly ExchangeRateApiClient _exchangeRateApiClient;
        private static readonly MemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());

        public WebRequestsProvider(
            ILogger<WebRequestsProvider> logger, IHttpClientFactory clientFactory, FaceSettings faceSettings, IMetrics metrics, IMapper mapper,
            CurrencyConverterClient currencyConverterClient, ExchangeRateApiClient exchangeRateApiClient)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _faceSettings = faceSettings;
            _metrics = metrics;
            _mapper = mapper;
            _currencyConverterClient = currencyConverterClient;
            _exchangeRateApiClient = exchangeRateApiClient;
        }



        /// <summary>
        /// Request weather info on DarkSky weather provider
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="token">ApiToken</param>
        /// <returns>Weather info <see cref="RequestDarkSky"/></returns>
        public async Task<WeatherInfo> RequestDarkSky(decimal lat, decimal lon, string token)
        {
            string providerName = WeatherProvider.DarkSky.ToString();
            _metrics.WeatherIncrement(providerName, SourceType.Remote);

            var client = _clientFactory.CreateClient(Options.DefaultName);
            using var response = await client.GetAsync(_faceSettings.BuildDarkSkyUrl(lat.ToString("G"), lon.ToString("G"), token));
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(response.StatusCode == HttpStatusCode.Unauthorized
                    ? $"Unauthorized access to {providerName}"
                    : $"Error {providerName} request, status: {response.StatusCode.ToString()}");
                return new WeatherInfo {RequestStatus = new RequestStatus(response.StatusCode)};
            }

            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);
            var weatherInfo = JsonSerializer.Deserialize<WeatherInfo>(
                json.RootElement.GetProperty("currently").GetRawText());
            weatherInfo.WeatherProvider = providerName;
            weatherInfo.RequestStatus = new RequestStatus(RequestStatusCode.Ok);

            return weatherInfo;
        }

        /// <summary>
        /// Request weather conditions from OpenWeather service
        /// </summary>
        /// <param name="lat">latitude</param>
        /// <param name="lon">longitude</param>
        /// <returns>Weather conditions for the specified coordinates </returns>
        public async Task<WeatherInfo> RequestOpenWeather(decimal lat, decimal lon)
        {
            var providerName = WeatherProvider.OpenWeather.ToString();
            _metrics.WeatherIncrement(providerName, SourceType.Remote);

            var conditionIcons = new Dictionary<string, string>
            {
                {"01d", "clear-day"}, {"01n", "clear-night"}, 
                {"10d", "rain"}, {"10n", "rain"}, {"09d", "rain"}, {"09n", "rain"},  {"11d", "rain"}, {"11n", "rain"},
                {"13d", "snow"}, {"13n", "snow"},  
                {"50d", "fog"}, {"50n", "fog"},
                {"03d","cloudy"}, {"03n","cloudy"}, 
                {"02d", "partly-cloudy-day"}, {"02n", "partly-cloudy-night"}, {"04d", "partly-cloudy-day"}, {"04n", "partly-cloudy-night"}
            };

            var client = _clientFactory.CreateClient(Options.DefaultName);
            using var response = await client.GetAsync(_faceSettings.BuildOpenWeatherUrl(lat.ToString("G"), lon.ToString("G")));
            if (!response.IsSuccessStatusCode)
            { 
                _logger.LogWarning(response.StatusCode == HttpStatusCode.Unauthorized
                    ? $"Unauthorized access to {providerName}"
                    : $"Error {providerName} request, status: {response.StatusCode.ToString()}");
                return new WeatherInfo {RequestStatus = new RequestStatus(response.StatusCode)};
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

            var weatherInfo = _mapper.Map<WeatherInfo>(elements);
            weatherInfo.WeatherProvider = providerName;
            weatherInfo.RequestStatus = new RequestStatus(RequestStatusCode.Ok);

            return weatherInfo;
        }




        /// <summary>
        /// Request current Exchange rate in local cache
        /// </summary>
        /// <param name="baseCurrency">the currency from which convert</param>
        /// <param name="targetCurrency">the currency to which convert</param>
        /// <returns>Returns exchange rate, if cache is missed returns null</returns>
        public async Task<ExchangeRateInfo> RequestCacheExchangeRate(
            string baseCurrency, string targetCurrency)
        {
            string cacheKey = $"er-{baseCurrency}-{targetCurrency}";
            if (MemoryCache.TryGetValue(cacheKey, out ExchangeRateInfo exchangeRateInfo))
            {
                _metrics.ExchangeRateIncrement("cache", SourceType.Memory, baseCurrency, targetCurrency);
                return exchangeRateInfo;
            }

            var fallbackPolicy = Policy<ExchangeRateInfo>
                .Handle<Exception>()
                .OrResult(_ => _.RequestStatus.StatusCode == RequestStatusCode.Error)
                .FallbackAsync(async cancellationToken =>
                {
                    if (_faceSettings.ExchangeRateSupportedCurrency.Contains(baseCurrency) &&
                        _faceSettings.ExchangeRateSupportedCurrency.Contains(targetCurrency))
                        return await _exchangeRateApiClient.RequestExchangeRateApi(baseCurrency, targetCurrency)
                            .ConfigureAwait(false);
                    return new ExchangeRateInfo();
                });
            exchangeRateInfo = await fallbackPolicy.ExecuteAsync(async () => 
                await _currencyConverterClient.RequestCurrencyConverter(baseCurrency, targetCurrency)
                .ConfigureAwait(false));
            
            if (exchangeRateInfo.RequestStatus.StatusCode == RequestStatusCode.Ok && exchangeRateInfo.ExchangeRate != 0)
                MemoryCache.Set(cacheKey, exchangeRateInfo, TimeSpan.FromMinutes(60));
            return exchangeRateInfo;
        }
    }
}
