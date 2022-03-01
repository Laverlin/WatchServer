using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using App.Metrics;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Abstract.Entity.WatchFace;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service.HttpClients
{
    /// <summary>
    /// Request current Exchange rate on api.exchangerateapi.com
    /// </summary>
    public class ExchangeRateApiClient
    {
        private readonly ILogger<ExchangeRateApiClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;

        public ExchangeRateApiClient(
            ILogger<ExchangeRateApiClient> logger, HttpClient httpClient, FaceSettings faceSettings, IMetrics metrics)
        {
            httpClient.BaseAddress = new Uri("https://api.exchangeratesapi.io");
            _logger = logger;
            _httpClient = httpClient;
            _faceSettings = faceSettings;
            _metrics = metrics;
        }


        /// <summary>
        /// Request current Exchange rate on api.exchangerateapi.com
        /// </summary>
        /// <param name="baseCurrency">the currency from which convert</param>
        /// <param name="targetCurrency">the currency to which convert</param>
        /// <returns>exchange rate. if conversion is unsuccessfull the rate could be 0 </returns>
        public virtual async Task<ExchangeRateInfo> RequestExchangeRateApi(string baseCurrency, string targetCurrency)
        {
            _metrics.ExchangeRateIncrement("ExchangeRateApi.com", SourceType.Remote, baseCurrency, targetCurrency);

            try {
                using var response = await _httpClient.GetAsync(_faceSettings.BuildExchangeRateApiUrl(baseCurrency, targetCurrency));
                if (!response.IsSuccessStatusCode)
                { 
                    _logger.LogWarning($"Error ExchangeRate request, status: {response.StatusCode}");
                    return new ExchangeRateInfo {RequestStatus = new RequestStatus(response.StatusCode)};
                }

                await using var content = await response.Content.ReadAsStreamAsync();
                using var json = await JsonDocument.ParseAsync(content);
                return new ExchangeRateInfo
                {
                    ExchangeRate = json.RootElement.GetProperty("rates")
                        .TryGetProperty($"{targetCurrency}", out var rate) 
                        ? rate.GetDecimal() : 0,
                    RequestStatus = new RequestStatus(RequestStatusCode.Ok)
                };
            }
            catch(Exception exception)
            {
                _logger.LogError($"Error ExchangeRate request: {exception.Message}");
                return new ExchangeRateInfo {RequestStatus = new RequestStatus(RequestStatusCode.Error)};
            }

        }
    }
}
