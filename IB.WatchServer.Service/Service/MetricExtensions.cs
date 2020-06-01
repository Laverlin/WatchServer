using System.Linq;
using App.Metrics;
using App.Metrics.Counter;

namespace IB.WatchServer.Service.Service
{
    /// <summary>
    /// Helper to increment request counters
    /// </summary>
    public static class MetricExtensions
    {
        private static readonly string[] _tagNames = {"provider", "source_type"};

        public static void ExchangeRateIncrement(
            this IMetrics metrics, string provider, SourceType sourceType, string baseCurrency, string targetCurrency)
        {
            metrics.Measure.Counter.Increment(
                new CounterOptions{Name = "exchangeRate-request", MeasurementUnit = Unit.Calls }, 
                new MetricTags(
                    _tagNames.Append("currency").ToArray(), 
                    new[] {provider, sourceType.ToString(), $"{baseCurrency}-{targetCurrency}"}));
        }

        public static void WeatherIncrement(
            this IMetrics metrics, string provider, SourceType sourceType)
        {
            metrics.Measure.Counter.Increment(
                new CounterOptions {Name = "weather-request", MeasurementUnit = Unit.Calls},
                new MetricTags(_tagNames, new[] {provider, sourceType.ToString()}));
        }

        public static void LocationIncrement(this IMetrics metrics, string provider, SourceType sourceType)
        {
            metrics.Measure.Counter.Increment(
                new CounterOptions {Name = "location-request", MeasurementUnit = Unit.Calls},
                new MetricTags(_tagNames, new[] {provider, sourceType.ToString()}));
        }
    }

    public enum SourceType
    {
        Remote,
        Database,
        Memory
    }
}