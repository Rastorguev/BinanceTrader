using Newtonsoft.Json;

namespace BinanceTrader.Core.Entities.Enums
{
    public enum OrderStatus
    {
        New,
        [JsonProperty(PropertyName = "PARTIALLY_FILLED")]
        PARTIALLY_FILLED,
        Filled,
        Canceled,
        [JsonProperty(PropertyName = "PENDING_CANCEL")]
        PENDING_CANCEL,
        Rejected,
        Expired
    }
}