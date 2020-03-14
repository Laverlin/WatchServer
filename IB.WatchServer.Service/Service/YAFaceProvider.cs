using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using AutoMapper;
using Microsoft.Extensions.Logging;

using LinqToDB;
using LinqToDB.Data;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure;
using LinqToDB.Tools;
using Microsoft.Extensions.Options;
using IB.WatchServer.Service.Entity.Settings;

namespace IB.WatchServer.Service.Service
{
    /// <summary>
    /// Provider for the Watch Face services
    /// </summary>
    public class YAFaceProvider
    {
        private readonly ILogger<YAFaceProvider> _logger;
        private readonly FaceSettings _faceSettings;
        private readonly IHttpClientFactory _clientFactory;
        private readonly DataConnectionFactory _dbFactory;
        private readonly IMapper _mapper;
        private readonly IMetrics _metrics;

        public YAFaceProvider(
            ILogger<YAFaceProvider> logger, IHttpClientFactory clientFactory, FaceSettings faceSettings, 
            DataConnectionFactory dbFactory, IMapper mapper, IMetrics metrics)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _faceSettings = faceSettings;
            _dbFactory = dbFactory;
            _mapper = mapper;
            _metrics = metrics;
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
        /// Return location name from geocode provider
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>Location name</returns>
        public async Task<string> RequestLocationName(string lat, string lon)
        {
            _metrics.Measure.Counter.Increment(new CounterOptions {Name = "locationRequest-external", MeasurementUnit = Unit.Calls});
            var client = _clientFactory.CreateClient(Options.DefaultName);
            using var response = await client.GetAsync(_faceSettings.BuildLocationUrl(lat, lon));
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
            _metrics.Measure.Counter.Increment(new CounterOptions {Name = "locationRequest-cache-db", MeasurementUnit = Unit.Calls});
            await using var db = _dbFactory.Create();
            var city = await db.GetTable<RequestInfo>().Where(c => c.RequestTime != null)
                .Join(db.GetTable<DeviceInfo>().Where(d => d.DeviceId == deviceId), c => c.DeviceInfoId, d => d.Id,
                    (c, d) => new {c.CityName, c.Lat, c.Lon, c.RequestTime})
                .OrderByDescending(c => c.RequestTime).Take(1)
                .Where(c => c.Lat == latitude && c.Lon == longitude)
                .SingleOrDefaultAsync();

           return city?.CityName;
        }

        /// <summary>
        /// Request weather info on DarkSky weather provider
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="token">ApiToken</param>
        /// <returns>Weather info <see cref="RequestDarkSky"/></returns>
        public async Task<WeatherResponse> RequestDarkSky(string lat, string lon, string token)
        {
            string providerName = WeatherProvider.DarkSky.ToString();
            _metrics.Measure.Counter.Increment(new CounterOptions {Name = "weatherRequest", MeasurementUnit = Unit.Calls}, providerName);

            var client = _clientFactory.CreateClient();
            using var response = await client.GetAsync(_faceSettings.BuildDarkSkyUrl(lat, lon, token));
            if (!response.IsSuccessStatusCode)
            { 
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException();

                throw new HttpRequestException($"Error service request, status: {response.StatusCode.ToString()}");
            }

            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);
            var weatherResponse = JsonSerializer.Deserialize<WeatherResponse>(
                json.RootElement.GetProperty("currently").GetRawText());
            weatherResponse.WeatherProvider = providerName;

            return weatherResponse;
        }

        /// <summary>
        /// Request weather conditions from OpenWeather service
        /// </summary>
        /// <param name="lat">latitude</param>
        /// <param name="lon">longitude</param>
        /// <returns>Weather conditions for the specified coordinates <see cref="WeatherResponse"/></returns>
        public async Task<WeatherResponse> RequestOpenWeather(string lat, string lon)
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
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException();

                throw new HttpRequestException($"Error OpenWeather request, status: {response.StatusCode.ToString()}");
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

            var weatherResponse = _mapper.Map<WeatherResponse>(elements);
            weatherResponse.WeatherProvider = providerName;

            return weatherResponse;
        }

        public async Task SaveRequestInfo(WatchFaceRequest watchFaceRequest, string cityName)
            => await SaveRequestInfo(RequestType.Location, watchFaceRequest, new WeatherResponse {CityName = cityName});


        /// <summary>
        /// Store Weather Request and response info in DB
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="watchFaceRequest">location data</param>
        /// <param name="weatherResponse">weather response</param>
        public async Task SaveRequestInfo(RequestType requestType, WatchFaceRequest watchFaceRequest, WeatherResponse weatherResponse)
        {
            await using var db = _dbFactory.Create();
            var deviceInfo = db.QueryProc<DeviceInfo>(
                    "add_device",
                    new DataParameter("device_id", watchFaceRequest.DeviceId ?? "unknown"),
                    new DataParameter("device_name", watchFaceRequest.DeviceName))
                .Single();

            var requestInfo = _mapper.Map<RequestInfo>(watchFaceRequest);
            requestInfo = _mapper.Map(weatherResponse, requestInfo);
            requestInfo.DeviceInfoId = deviceInfo.Id;
            requestInfo.RequestTime = DateTime.UtcNow;
            requestInfo.RequestType = requestType;

            await db.GetTable<RequestInfo>().DataContext.InsertAsync(requestInfo);

            _logger.LogDebug("{@requestInfo}", requestInfo);
        }
    }
}
