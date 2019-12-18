using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net;

namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Trottling attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestRateLimitAttribute : ActionFilterAttribute
    {
        private static MemoryCache MemoryCache { get; } = new MemoryCache(new MemoryCacheOptions());

        public int Seconds { get; set; }

        public string KeyField { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //base.OnActionExecuting(context);

            string keyValue = context.HttpContext.Request.Query[KeyField];

            string name = "device-key";
            string memoryCacheKey = $"{name}-{keyValue}";
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
                //Log.Warning("Too many requests from device {KeyValue}", keyValue);
            }
        }
    }
}
