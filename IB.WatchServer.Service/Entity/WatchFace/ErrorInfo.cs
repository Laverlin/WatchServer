using System.Net;
using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    /// <summary>
    /// Description of error during request to an external service
    /// </summary>
    public class ErrorInfo
    {
        public ErrorInfo(HttpStatusCode httpStatusCode)
        {
            IsError = true;
            ErrorDescription = httpStatusCode.ToString();
            ErrorCode = (int) httpStatusCode;
        }

        /// <summary>
        /// Is there any error
        /// </summary>
        [JsonPropertyName("isError")]
        public bool IsError { get; set; } = false;

        /// <summary>
        /// Text description of the error
        /// </summary>
        [JsonPropertyName("errorDescription")]
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Error code. In most cases, http status code returned by external service
        /// </summary>
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }
    }
}
