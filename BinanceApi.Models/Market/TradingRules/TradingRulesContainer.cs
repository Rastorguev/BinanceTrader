using System.Collections.Generic;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Market.TradingRules
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
        [JsonConverter(typeof(ConcreteTypeConverter<IEnumerable<TradingRules>>))]
        public IEnumerable<ITradingRules> Rules { get; set; }
    }
}