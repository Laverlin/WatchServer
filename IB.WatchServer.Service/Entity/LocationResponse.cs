using IB.WatchServer.Service.Infrastructure;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Represents the location description
    /// </summary>
    public class LocationResponse
    {
        public string Version
        {
            get
            {
                return SolutionInfo.GetVersion();
            }
        }

        /// <summary>
        /// The Name of the plase 
        /// </summary>
        public string CityName { get; set; }
    }
}
