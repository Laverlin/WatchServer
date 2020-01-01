using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using LinqToDB;
using LinqToDB.Data;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure.Linq2DB;

namespace IB.WatchServer.Service.Service
{
    /// <summary>
    /// Provider for the Watch Face services
    /// </summary>
    public class YAFaceProvider
    {
        //private readonly PostgresSettings _postgresSettings;
        private readonly ILogger<YAFaceProvider> _logger;
        private readonly FaceSettings _faceSettings;
        private readonly IHttpClientFactory _clientFactory;
        private readonly DataConnectionFactory _dbFactory;

        public YAFaceProvider(
            ILogger<YAFaceProvider> logger, IHttpClientFactory clientFactory, FaceSettings faceSettings, DataConnectionFactory dbFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _faceSettings = faceSettings;
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Get count of unique devices fixed in DB
        /// </summary>
        public async Task<long> GetDeviceCount()
        {
            await using var db = _dbFactory.Create();
            return await db.GetTable<DeviceInfo>().CountAsync();
        }

        /// <summary>
        /// Eliminate diacritics from given string
        /// </summary>
        /// <param name="text">input text with diacritics</param>
        /// <returns>ascii text or null if null has ben passed</returns>
        public string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Return location name from geocode provider
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>Location name</returns>
        public async Task<string> RequestLocationName(string lat, string lon)
        {
            var url = string.Format(_faceSettings.BaseUrl, lat, lon, _faceSettings.ApiKey);

            var client = _clientFactory.CreateClient();
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error service request, status: {response.StatusCode.ToString()}");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var resource = document.RootElement
                .GetProperty("resourceSets")[0]
                .GetProperty("resources");

            var city = (resource.GetArrayLength() > 0)
                ? resource[0].GetProperty("name").GetString()
                : null;

            return city;
        }

        /// <summary>
        /// Search in DB the last location of this device. If location is the same then City name will be returned,
        /// otherwise null
        /// </summary>
        /// <param name="deviceId">Garmin device id</param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns>City name or null</returns>
        public async Task<string> CheckLastLocation(string deviceId, decimal latitude, decimal longitude)
        {
            await using var db = _dbFactory.Create();
            var city = await db.GetTable<RequestInfo>().Where(c=>c.RequestTime != null)
                .Join(db.GetTable<DeviceInfo>().Where(d => d.DeviceId == deviceId), c => c.DeviceInfoId, d => d.Id,
                    (c, d) => new {c.CityName, c.Lat, c.Lon, c.RequestTime})
                .OrderByDescending(c => c.RequestTime).Take(1)
                .Where(c=>c.Lat == latitude && c.Lon == longitude)
                .SingleOrDefaultAsync();

           return city?.CityName;
        }

        /// <summary>
        /// Return weather info
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>Weather info <see cref="RequestWeather"/></returns>
        public async Task<WeatherResponse> RequestWeather(string lat, string lon)
        {
            var serviceUrl = string.Format(_faceSettings.WeatherBaseUrl, _faceSettings.WeatherApiKey, lat, lon);
            var client = _clientFactory.CreateClient();
            using var response = await client.GetAsync(serviceUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error service request, status: {response.StatusCode.ToString()}");
            }

            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);
            var weatherResponse = JsonSerializer.Deserialize<WeatherResponse>(
                json.RootElement.GetProperty("currently").GetRawText());
            
            return weatherResponse;
        }

        public async Task SaveRequestInfo(
            WatchFaceRequest watchFaceRequest, string cityName)
            => await SaveRequestInfo(RequestType.Location, watchFaceRequest, new WeatherResponse {CityName = cityName});


        /// <summary>
        /// Store Weather Request and response info in DB
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="watchFaceRequest">location data</param>
        /// <param name="weatherResponse">weather response</param>
        public async Task SaveRequestInfo(RequestType requestType, WatchFaceRequest watchFaceRequest, WeatherResponse weatherResponse)
        {
            string deviceId = watchFaceRequest.DeviceId ?? "unknown";

            await using var db = _dbFactory.Create();
            var deviceInfo = db.QueryProc<DeviceInfo>(
                    "add_device",
                    new DataParameter("device_id", deviceId),
                    new DataParameter("device_name", watchFaceRequest.DeviceName))
                .Single();

            var requestInfo = new RequestInfo
            {
                DeviceInfoId = deviceInfo.Id,
                CityName = weatherResponse.CityName,
                Lat = Convert.ToDecimal(watchFaceRequest.Lat),
                Lon = Convert.ToDecimal(watchFaceRequest.Lon),
                RequestTime = DateTime.UtcNow,
                Version = watchFaceRequest.Version,
                Framework = watchFaceRequest.Framework,
                CiqVersion = watchFaceRequest.CiqVersion,
                RequestType = requestType,
                Temperature = weatherResponse.Temperature,
                Wind = weatherResponse.WindSpeed,
                PrecipProbability = weatherResponse.PrecipProbability
            };
            requestInfo.DeviceInfoId = deviceInfo.Id;
            await db.GetTable<RequestInfo>().DataContext.InsertAsync(requestInfo);

            _logger.LogDebug("{@requestInfo}", requestInfo);
        }
    }
}
