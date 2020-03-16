using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    public class LocationInfo 
    {
        public LocationInfo(){}

        public LocationInfo(string cityName)
        {
            CityName = cityName;
            RequestStatus = new RequestStatus(RequestStatusCode.Ok);
        }

        [JsonPropertyName("cityName")]
        public string CityName { get; set;}

        [JsonPropertyName("status")]
        public RequestStatus RequestStatus { get; set; } = new RequestStatus();
    }
}