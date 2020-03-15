using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    public class LocationInfo 
    {
        [JsonPropertyName("cityName")]
        public string CityName { get; set;}

        [JsonPropertyName("errorInfo")]
        public ErrorInfo ErrorInfo { get; set; }
    }
}