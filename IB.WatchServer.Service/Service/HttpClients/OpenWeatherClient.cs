using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using App.Metrics;
using AutoMapper;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using LinqToDB.Tools;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service.HttpClients
{
    public class OpenWeatherClient
    {
        private readonly ILogger<OpenWeatherClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;
        private readonly IMapper _mapper;

        public OpenWeatherClient(
            ILogger<OpenWeatherClient> logger, HttpClient httpClient, FaceSettings faceSettings, IMetrics metrics, IMapper mapper)
        {
            httpClient.BaseAddress = new Uri("https://api.openweathermap.org");
            _logger = logger;
            _httpClient = httpClient;
            _faceSettings = faceSettings;
            _metrics = metrics;
            _mapper = mapper;
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

            using var response = await _httpClient.GetAsync(_faceSettings.BuildOpenWeatherUrl(lat, lon));
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
    }
}
