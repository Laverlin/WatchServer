using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;

namespace IB.WatchServer.Service.Service
{
    public static class HttpBuilderExtensions
    {
        /// <summary>
        /// Add Retry policy with 3 attempts before exception
        /// </summary>
        /// <param name="builder">IHttpClientBuilder</param>
        public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder builder)
        {
            return builder.AddPolicyHandler((serviceProvider, request) => HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt * 3),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<ExchangeRateCacheStrategy>();
                        logger.LogWarning("Delaying for {delay}ms, then making retry {retry}. CorrelationId {correlationId}",
                            timespan.TotalMilliseconds, retryAttempt, context.CorrelationId);
                    }));
        }

        /// <summary>
        /// Add retry policy with 3 attempts and then circuite breaker policy 
        /// </summary>
        /// <param name="builder"><see cref="IHttpClientBuilder"/></param>
        /// <param name="attempts">Number of attempts before circuite breaker shell open</param>
        /// <param name="timeout">Time period while cb remains open</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRetryPolicyWithCb(this IHttpClientBuilder builder, int attempts, TimeSpan timeout)
        {
            return builder.AddRetryPolicy().AddTransientHttpErrorPolicy(_ => _.CircuitBreakerAsync(attempts, timeout));
        }
    }
}
