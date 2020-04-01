using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using App.Metrics;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Entity.WatchFace;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service.HttpClients
{
    /// <summary>
    /// Request current Exchange rate on  currencyconverterapi.com
    /// </summary>
    public class CurrencyConverterClient
    {
        private readonly ILogger<CurrencyConverterClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;

        public CurrencyConverterClient(
            ILogger<CurrencyConverterClient> logger, HttpClient httpClient, FaceSettings faceSettings, IMetrics metrics)
        {
            httpClient.BaseAddress = new Uri("https://free.currconv.com");
            _logger = logger;
            _httpClient = httpClient;
            _faceSettings = faceSettings;
            _metrics = metrics;
        }

        /// <summary>
        /// Request current Exchange rate on  currencyconverterapi.com
        /// </summary>
        /// <param name="baseCurrency">the currency from which convert</param>
        /// <param name="targetCurrency">the currency to which convert</param>
        /// <returns>exchange rate. if conversion is unsuccessfull the rate could be 0 </returns>
        public virtual async Task<ExchangeRateInfo> RequestCurrencyConverter(string baseCurrency, string targetCurrency)
        {
            _metrics.ExchangeRateIncrement("CurrencyConverter.com", SourceType.Remote, baseCurrency, targetCurrency);

            using var response = await _httpClient.GetAsync(_faceSettings.BuildCurrencyConverterUrl(baseCurrency, targetCurrency));
            if (!response.IsSuccessStatusCode)
            { 
                _logger.LogWarning(response.StatusCode == HttpStatusCode.Unauthorized
                    ? $"Unauthorized access to currencyconverterapi.com"
                    : $"Error currencyconverterapi.com request, status: {response.StatusCode.ToString()}");
                return new ExchangeRateInfo {RequestStatus = new RequestStatus(response.StatusCode)};
            }

            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);

            return new ExchangeRateInfo
            {
                ExchangeRate = json.RootElement.TryGetProperty($"{baseCurrency}_{targetCurrency}", out var rate) 
                    ? rate.GetDecimal() : 0,
                RequestStatus = new RequestStatus(RequestStatusCode.Ok)
            };
        }
    }
}
