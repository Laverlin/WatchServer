using Microsoft.AspNetCore.Authentication;

namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Options for schema authentication
    /// </summary>
    public class TokenAuthOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Secret token provided by client
        /// </summary>
        public string ApiToken { get; set; }

        /// <summary>
        /// Scheme name
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Query parameter name where to find the token
        /// </summary>
        public string ApiTokenName { get; set; }
    }
}