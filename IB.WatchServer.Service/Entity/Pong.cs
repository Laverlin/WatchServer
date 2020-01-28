
namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Response for the health check request
    /// </summary>
    public class Pong : BaseApiResponse
    {
        /// <summary>
        /// Total amount of the device in db
        /// </summary>
        public long DeviceCount { get; set; }
    }
}
