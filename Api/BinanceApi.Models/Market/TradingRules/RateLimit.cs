using Newtonsoft.Json;

namespace BinanceApi.Models.Market.TradingRules
{
    public class RateLimit
    {
        [JsonProperty("rateLimitType")]
        public string RateLimitType { get; set; }

        [JsonProperty("interval")]
        public string Interval { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
    }
}