using System.Net;
using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    /// <summary>
    /// Description of result of the request to an external service
    /// </summary>
    public class RequestStatus
    {
        public RequestStatus()
        {
        }

        public RequestStatus(RequestStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public RequestStatus(HttpStatusCode httpStatusCode)
        {
            StatusCode = RequestStatusCode.Error;
            ErrorDescription = httpStatusCode.ToString();
            ErrorCode = (int) httpStatusCode;
        }

        [JsonPropertyName("statusCode")]
        public RequestStatusCode StatusCode { get; set; } = RequestStatusCode.HasNotBeenRequested;

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

    public enum RequestStatusCode
    {
        Error = -1,
        HasNotBeenRequested = 0,
        Ok = 1
    }
}
