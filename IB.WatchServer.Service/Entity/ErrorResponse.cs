namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// All error responses should be described by this class
    /// </summary>
    public class ErrorResponse : BaseApiResponse
    {
        /// <summary>
        /// Error Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Status Code
        /// </summary>
        public int StatusCode { get; set; }
    }
}
