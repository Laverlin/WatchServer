using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using App.Metrics;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Abstract.Entity.WatchFace;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service.HttpClients
{
    /// <summary>
    /// Http Client to work with https://dev.virtualearth.net
    /// </summary>
    public class VirtualearthClient
    {
        private class LocationCache
        {
            public string LocationName { get; set; }
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
        }

        private readonly ILogger<VirtualearthClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;
        private static readonly MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

        public VirtualearthClient(
            ILogger<VirtualearthClient> logger, HttpClient httpClient, FaceSettings faceSettings, IMetrics metrics)
        {
            httpClient.BaseAddress = new Uri("https://dev.virtualearth.net");
            _logger = logger;
            _httpClient = httpClient;
            _faceSettings = faceSettings;
            _metrics = metrics;
        }

        /// <summary>
        /// Request LocationName on VirtualEarth
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>Location Name info <see cref="LocationInfo"/></returns>
        public async Task<LocationInfo> RequestLocationName(decimal lat, decimal lon)
        {
            _metrics.LocationIncrement("virtualearth", SourceType.Remote);

            using var response = await _httpClient.GetAsync(_faceSettings.BuildLocationUrl(lat, lon));
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(response.StatusCode == HttpStatusCode.Unauthorized
                    ? $"Unauthorized access to virtualearth"
                    : $"Error virtualearth request, status: {response.StatusCode.ToString()}");
                return new LocationInfo {RequestStatus = new RequestStatus(response.StatusCode)};
            }

            var content = await response.Content.ReadAsStreamAsync();
            using var document = JsonDocument.Parse(content);
            var resource = document.RootElement
                .GetProperty("resourceSets")[0]
                .GetProperty("resources");

            var city = (resource.GetArrayLength() > 0)
                ? resource[0].GetProperty("name").GetString()
                : null;

            return new LocationInfo(city);
        }

        public async Task<LocationInfo> GetCachedLocationName(string deviceId, decimal lat, decimal lon)
        {
            string cacheKey = $"loc-{deviceId}";
            if (_memoryCache.TryGetValue(cacheKey, out LocationCache locationCache))
            {
                if (locationCache.Latitude == lat && locationCache.Longitude == lon)
                {
                    _metrics.LocationIncrement("cache", SourceType.Memory);
                    return new LocationInfo(locationCache.LocationName);
                }

                _memoryCache.Remove(cacheKey);
            }

            var locationInfo = await RequestLocationName(lat, lon);
            _memoryCache.Set(
                cacheKey,
                new LocationCache {LocationName = locationInfo.CityName, Latitude = lat, Longitude = lon},
                TimeSpan.FromHours(24));

            return locationInfo;
        }
    }
}
