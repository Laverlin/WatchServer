using System.Text.Json.Serialization;


namespace IB.WatchServer.Service.Entity.WatchFace
{
    public class BaseResponseInfo
    {
        /// <summary>
        /// Indicates the successfulness of request  
        /// </summary>
        [JsonPropertyName("isError")]
        public bool IsError { get; set; }

        /// <summary>
        /// The http status code of request to the external service
        /// </summary>
        [JsonPropertyName("httpStatusCode")]
        public int HttpStatusCode { get; set; }
    }
}
