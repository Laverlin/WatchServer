using System.Text.Json.Serialization;

namespace IB.WatchServer.Abstract.Entity
{
    /// <summary>
    /// Base contract for all api responses
    /// </summary>
    public abstract class BaseApiResponse
    {
        /// <summary>
        /// API Version number
        /// </summary>
        [JsonPropertyName("serverVersion")]
        public string ServerVersion => SolutionInfo.Version;
    }
}
