using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using App.Metrics;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service.HttpClients
{
    public class DarkSkyClient
    {
        private readonly ILogger<DarkSkyClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;

        /// <summary>
        /// Request weather info on DarkSky weather provider
        /// </summary>
        public DarkSkyClient(
            ILogger<DarkSkyClient> logger, HttpClient httpClient, FaceSettings faceSettings, IMetrics metrics)
        {
            httpClient.BaseAddress = new Uri("https://api.darksky.net");
            _logger = logger;
            _httpClient = httpClient;
            _faceSettings = faceSettings;
            _metrics = metrics;
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

            using var response = await _httpClient.GetAsync(_faceSettings.BuildDarkSkyUrl(lat, lon, token));
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
    }
}
