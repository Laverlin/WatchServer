using App.Metrics;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Service.Entity.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace IB.WatchServer.Service.Service.HttpClients
{
    public class ExchangeRateHostClient
    {
        private readonly ILogger<ExchangeRateApiClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly FaceSettings _faceSettings;
        private readonly IMetrics _metrics;

        public ExchangeRateHostClient(
            ILogger<ExchangeRateApiClient> logger, HttpClient httpClient, FaceSettings faceSettings, IMetrics metrics)
        {
            httpClient.BaseAddress = new Uri("https://api.exchangerate.host");
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
        public virtual async Task<ExchangeRateInfo> RequestExchangeRateHostApi(string baseCurrency, string targetCurrency)
        {
            _metrics.ExchangeRateIncrement("exchangerate.host", SourceType.Remote, baseCurrency, targetCurrency);

            try {
                using var response = await _httpClient.GetAsync(_faceSettings.BuildExchangeHostApiUrl(baseCurrency, targetCurrency));
                if (!response.IsSuccessStatusCode)
                { 
                    _logger.LogWarning($"Error ExchangeHost request, status: {response.StatusCode}");
                    return new ExchangeRateInfo {RequestStatus = new RequestStatus(response.StatusCode)};
                }

                await using var content = await response.Content.ReadAsStreamAsync();
                using var json = await JsonDocument.ParseAsync(content);
                if (json.RootElement.TryGetProperty("info", out var info))
                    return new ExchangeRateInfo
                    {
                        ExchangeRate = info.TryGetProperty($"rate", out var rate) 
                            ? rate.GetDecimal() : 0,
                        RequestStatus = new RequestStatus(RequestStatusCode.Ok)
                    };
                else
                    return new ExchangeRateInfo
                    {
                        ExchangeRate = 0,
                        RequestStatus = new RequestStatus(RequestStatusCode.Error)
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
