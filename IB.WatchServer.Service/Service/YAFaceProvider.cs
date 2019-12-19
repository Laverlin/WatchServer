using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;

using IB.WatchServer.Service.Entity;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System;

namespace IB.WatchServer.Service.Service
{
    /// <summary>
    /// Provider for the Watch Face services
    /// </summary>
    public class YAFaceProvider
    {
        private readonly PostgresSettings _postgresSettings;
        private readonly ILogger<YAFaceProvider> _logger;
        private readonly FaceSettings _locationSetting;
        private readonly IHttpClientFactory _clientFactory;

        public YAFaceProvider(
            ILogger<YAFaceProvider> logger, IHttpClientFactory clientFactory, FaceSettings locationSetting, PostgresSettings postgresSettings)
        {
            _logger = logger;
            _postgresSettings = postgresSettings;
            _clientFactory = clientFactory;
            _locationSetting = locationSetting;
        }

        /// <summary>
        /// Get count of unique devices fixed in DB
        /// </summary>
        public async Task<long> GetDeviceCount()
        {
            using var db = new DataConnection(new PostgreSQLDataProvider(), _postgresSettings.ConnectionString);
            return await db.GetTable<DeviceInfo>().CountAsync();
        }

        /// <summary>
        /// Save location request to the DB
        /// </summary>
        /// <param name="deviceId">unique device id</param>
        /// <param name="deviceName">device name</param>
        /// <param name="cityInfo"><see cref="CityInfo"/> City info with request data</param>
        public async Task SaveRequest(string deviceId, string deviceName, CityInfo cityInfo)
        {
            using var db = new DataConnection(new PostgreSQLDataProvider(), _postgresSettings.ConnectionString);
            var deviceInfo = db.QueryProc<DeviceInfo>(
                "add_device", new DataParameter("device_id", deviceId), new DataParameter("device_name", deviceName))
                .Single();
            cityInfo.DeviceInfoId = deviceInfo.Id;
            await db.GetTable<CityInfo>().DataContext.InsertAsync(cityInfo);

            _logger.LogDebug($"Id : {deviceInfo.Id}, DeviceId : {deviceInfo.DeviceId}");
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
            var url = string.Format(_locationSetting.BaseUrl, lat, lon, _locationSetting.ApiKey);

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
    }
}
