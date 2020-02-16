using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace IB.WatchServer.Service.Infrastructure
{
    public static class AppMetricsExtension
    {
        /// <summary>
        /// AppMetrics out of the box does not support api routes metrics, since up until core 3.0 there was no clear way to work with it.
        /// So, additional extension method is needed to support this
        /// </summary>
        public static void UseAppMetricsEndpointRoutesResolver(this IApplicationBuilder app)
        {
            app.Use((context, next) =>
            {
                var metricsCurrentRouteName = "__App.Metrics.CurrentRouteName__";
                var endpointFeature = context.Features[typeof(IEndpointFeature)] as IEndpointFeature;
                if (endpointFeature?.Endpoint is RouteEndpoint endpoint)
                {
                    var method = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()
                        ?.HttpMethods
                        ?.FirstOrDefault();
                    var routePattern = endpoint.RoutePattern?.RawText;
                    var templateRouteDetailed = $"{method} {routePattern} {endpoint.DisplayName}";
                    var templateRoute = $"{method} {routePattern}";
                    if (!context.Items.ContainsKey(metricsCurrentRouteName))
                    {
                        context.Items.Add(metricsCurrentRouteName, templateRouteDetailed);
                    }
                }

                return next();
            });
        }
    }
}
