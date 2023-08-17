using System.Collections.Generic;
using Newtonsoft.Json;

namespace BinanceApi.Models.Market.TradingRules
{
    public class TradingRulesContainer
    {
        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("serverTime")]
        public long ServerTime { get; set; }

        [JsonProperty("rateLimits")]
        public IEnumerable<RateLimit> RateLimits { get; set; }

        [JsonProperty("symbols")]
        public IEnumerable<TradingRules> Rules { get; set; }
    }
}