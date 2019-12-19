using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Trottling attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestRateLimitAttribute : ActionFilterAttribute
    {
        private static MemoryCache MemoryCache { get; } = new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// The number of seconds during that subsequent requests from the same source will be prevented
        /// </summary>
        public int Seconds { get; set; }

        /// <summary>
        /// The name of the field to unique identify the request source
        /// </summary>
        public string KeyField { get; set; }

        /// <summary>
        /// Logger instance
        /// </summary>
        public ILogger<RequestRateLimitAttribute> Logger { get; set; }

        /// <summary>
        /// Actual execution
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            string keyValue = context.HttpContext.Request.Query[KeyField];
            if (string.IsNullOrEmpty(keyValue))
                return;

            string memoryCacheKey = $"device-key-{keyValue}";

            if (!MemoryCache.TryGetValue(memoryCacheKey, out bool _))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(Seconds));
                MemoryCache.Set(memoryCacheKey, true, cacheEntryOptions);
            }
            else
            {
                context.Result = new ContentResult { Content = "Too many requests" };
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                if (Logger != null)
                    Logger.LogWarning("Too many requests from {KeyValue}", keyValue);
            }
        }
    }
}
