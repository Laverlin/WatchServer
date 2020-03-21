using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.V1;
using IB.WatchServer.Service.Entity.WatchFace;

namespace IB.WatchServer.Service.Service
{
    public interface IYAFaceProvider
    {
        /// <summary>
        /// Return location name from geocode provider
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>Location name</returns>
        Task<string> RequestLocationName(string lat, string lon);

        /// <summary>
        /// Search in DB the last location of this device. If location is the same then City name will be returned,
        /// otherwise null
        /// </summary>
        /// <param name="deviceId">Garmin device id</param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns>City name or null</returns>
        Task<string> CheckLastLocation(string deviceId, decimal latitude, decimal longitude);

        /// <summary>
        /// Request weather info on DarkSky weather provider
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="token">ApiToken</param>
        /// <returns>Weather info <see cref="YAFaceProvider.RequestDarkSky"/></returns>
        Task<WeatherResponse> RequestDarkSky(string lat, string lon, string token);

        /// <summary>
        /// Request weather conditions from OpenWeather service
        /// </summary>
        /// <param name="lat">latitude</param>
        /// <param name="lon">longitude</param>
        /// <returns>Weather conditions for the specified coordinates <see cref="WeatherResponse"/></returns>
        Task<WeatherResponse> RequestOpenWeather(string lat, string lon);

        /// <summary>
        /// Store Weather Request and response info in DB
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="watchFaceRequest">location data</param>
        /// <param name="weatherResponse">weather response</param>
        Task SaveRequestInfo(RequestType requestType, WatchFaceRequest watchFaceRequest, WeatherResponse weatherResponse);
    }
}