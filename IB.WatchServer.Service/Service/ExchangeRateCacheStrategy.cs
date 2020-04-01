using App.Metrics;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using System;
using System.Linq;
using System.Threading.Tasks;
using IB.WatchServer.Service.Service.HttpClients;

namespace IB.WatchServer.Service.Service
{
    /// <summary>
    /// Define strategy of caching exchange rate results
    /// </summary>
    public class ExchangeRateCacheStrategy
    {
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;
        private readonly CurrencyConverterClient _currencyConverterClient;
        private readonly ExchangeRateApiClient _exchangeRateApiClient;
        private static readonly MemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());

        public ExchangeRateCacheStrategy(
            CurrencyConverterClient currencyConverterClient, ExchangeRateApiClient exchangeRateApiClient,
            FaceSettings faceSettings, IMetrics metrics)
        {
            _faceSettings = faceSettings;
            _metrics = metrics;
            _currencyConverterClient = currencyConverterClient;
            _exchangeRateApiClient = exchangeRateApiClient;
        }

        /// <summary>
        /// Request current Exchange rate in local cache. If not found, try CurrencyConverter, if fails - try ExchangeRateApi.io
        /// </summary>
        /// <param name="baseCurrency">the currency from which convert</param>
        /// <param name="targetCurrency">the currency to which convert</param>
        /// <returns>Returns exchange rate <see cref="ExchangeRateInfo"/></returns>
        public async Task<ExchangeRateInfo> GetExchangeRate(string baseCurrency, string targetCurrency)
        {
            string cacheKey = $"er-{baseCurrency}-{targetCurrency}";
            if (MemoryCache.TryGetValue(cacheKey, out ExchangeRateInfo exchangeRateInfo))
            {
                _metrics.ExchangeRateIncrement("cache", SourceType.Memory, baseCurrency, targetCurrency);
                return exchangeRateInfo;
            }

            var fallbackPolicy = Policy<ExchangeRateInfo>
                .Handle<Exception>()
                .OrResult(_ => _.RequestStatus.StatusCode == RequestStatusCode.Error)
                .FallbackAsync(async cancellationToken =>
                {
                    if (_faceSettings.ExchangeRateSupportedCurrency.Contains(baseCurrency) &&
                        _faceSettings.ExchangeRateSupportedCurrency.Contains(targetCurrency))
                        return await _exchangeRateApiClient.RequestExchangeRateApi(baseCurrency, targetCurrency)
                            .ConfigureAwait(false);
                    return new ExchangeRateInfo();
                });
            exchangeRateInfo = await fallbackPolicy.ExecuteAsync(async () =>
                await _currencyConverterClient.RequestCurrencyConverter(baseCurrency, targetCurrency)
                    .ConfigureAwait(false));
            
            if (exchangeRateInfo.RequestStatus.StatusCode == RequestStatusCode.Ok && exchangeRateInfo.ExchangeRate != 0)
                MemoryCache.Set(cacheKey, exchangeRateInfo, TimeSpan.FromMinutes(60));
            return exchangeRateInfo;
        }
    }
}
