
using System.Text.Json.Serialization;
using IB.WatchServer.Service.Entity;

namespace IB.WatchServer.XUnitTest.IntegrationTests
{
    /// <summary>
    /// Represents the location description
    /// </summary>
    public class LocationResponse : BaseApiResponse
    {
        /// <summary>
        /// The Name of the plase 
        /// </summary>
        [JsonPropertyName("cityName")]
        public string CityName { get; set; }
    }
}
