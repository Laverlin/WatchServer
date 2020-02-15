using IB.WatchServer.Service.Infrastructure;
using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Base contract for all api responses
    /// </summary>
    public abstract class BaseApiResponse
    {
        /// <summary>
        /// API Version number
        /// </summary>
        [JsonPropertyName("apiVersion")]
        public string ApiVersion => SolutionInfo.Version;
    }
}
