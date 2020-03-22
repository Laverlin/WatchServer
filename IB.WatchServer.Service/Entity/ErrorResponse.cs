using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// All error responses should be described by this class
    /// </summary>
    public class ErrorResponse : BaseApiResponse, IErrorResponseProvider
    {
        /// <summary>
        /// Error Description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// HTTP status Code
        /// </summary>
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// HTTP Status code text
        /// </summary>
        [JsonPropertyName("statusMessage")]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Generate error response related to API versioning
        /// </summary>
        /// <param name="context">Contextual information used when generating HTTP error responses related to API versioning</param>
        public IActionResult CreateResponse(ErrorResponseContext context)
        {
            return new ObjectResult(new ErrorResponse
            {
                StatusCode = context.StatusCode,
                StatusMessage = context.ErrorCode,
                Description = context.Message
            })
            {
                StatusCode = context.StatusCode
            };
        }
    }
}
