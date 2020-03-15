using System.Text.Json.Serialization;

namespace IB.WatchServer.Service.Entity.WatchFace
{
    public class ExchangeRateInfo 
    {
 //       [JsonPropertyName("baseCurrency")]
//        public string BaseCurrency { get; set; }

     //   [JsonPropertyName("targetCurrency")]
     //   public string TargetCurrency { get; set; }

        [JsonPropertyName("exchangeRate")]
        public decimal ExchangeRate { get; set; }

        [JsonPropertyName("errorInfo")]
        public ErrorInfo ErrorInfo { get; set; }
    }
}
