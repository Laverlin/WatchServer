using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace IB.WatchServer.Service.Service
{
    public static class HttpBuilderExtensions
    {
        public static string DefaultClientName => Options.DefaultName;

        public static string ExchangeClientName => "Exchange";

        public static IHttpClientBuilder DefaultHttpPolicy(this IHttpClientBuilder builder)
        {
            return builder.AddPolicyHandler((serviceProvider, request) => HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt * 3),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<ExchangeRateCacheStrategy>();
                        logger.LogWarning("Delaying for {delay}ms, then making retry {retry}. CorrelationId {correlationId}",
                            timespan.TotalMilliseconds, retryAttempt, context.CorrelationId);
                    }
                ));
        }

        public static IHttpClientBuilder CircuitHttpPolicy(this IHttpClientBuilder builder, int attempts, TimeSpan timeout)
        {
            return builder.AddTransientHttpErrorPolicy(_ => _.CircuitBreakerAsync(attempts, timeout));
        }
    }
}
