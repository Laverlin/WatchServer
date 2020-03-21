
using IB.WatchServer.Service.Entity;

namespace IB.WatchServer.Test.ControllerTest
{
    /// <summary>
    /// Represents the location description
    /// </summary>
    public class LocationResponse : BaseApiResponse
    {
        /// <summary>
        /// The Name of the plase 
        /// </summary>
        public string CityName { get; set; }
    }
}
