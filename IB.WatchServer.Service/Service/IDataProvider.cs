using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.WatchFace;

namespace IB.WatchServer.Service.Service
{
    public interface IDataProvider
    {
        /// <summary>
        /// Store Weather Request and response info in DB
        /// </summary>
        /// <param name="watchRequest">Watch request data</param>
        /// <param name="weatherInfo">Weather data</param>
        /// <param name="locationInfo">Location data</param>
        /// <param name="exchangeRateInfo">Exchange rate data</param>
        Task SaveRequestInfo(
            WatchRequest watchRequest, WeatherInfo weatherInfo, LocationInfo locationInfo, ExchangeRateInfo exchangeRateInfo);

        /// <summary>
        /// Search in DB the last location of this device. If location is the same the LocationInfo with City name will be returned,
        /// otherwise null
        /// </summary>
        /// <param name="deviceId">Garmin device id</param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns><see cref="LocationInfo"/> with CityName or null</returns>
        Task<LocationInfo> LoadLastLocation(string deviceId, decimal latitude, decimal longitude);

    }
}