using IB.WatchServer.Service.Infrastructure;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Response for the health check request
    /// </summary>
    public class Pong
    {
        /// <summary>
        /// Version of the assembly
        /// </summary>
        public string Version
        {
            get { return SolutionInfo.GetVersion(); }
        }

        /// <summary>
        /// Total amount of the device in db
        /// </summary>
        public long DeviceCount { get; set; }
    }
}
