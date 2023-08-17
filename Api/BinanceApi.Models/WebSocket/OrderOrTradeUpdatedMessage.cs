using Binance.API.Csharp.Client.Models.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Binance.API.Csharp.Client.Models.WebSocket
{
    public class OrderOrTradeUpdatedMessage : IWebSocketMessage, IOrder
    {
        [JsonProperty("c")]
        public string NewClientOrderId { get; set; }

        [JsonProperty("f")]
        public string TimeInForce { get; set; }

        [JsonProperty("P")]
        public decimal StopPrice { get; set; }

        [JsonProperty("x")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ExecutionType ExecutionType { get; set; }

        [JsonProperty("r")]
        public string RejectReason { get; set; }

        [JsonProperty("l")]
        public decimal LastFilledTradeQuantity { get; set; }

        [JsonProperty("z")]
        public decimal FilledTradesAccumulatedQuantity { get; set; }

        [JsonProperty("L")]
        public decimal LastFilledTradePrice { get; set; }

        [JsonProperty("n")]
        public decimal Commission { get; set; }

        [JsonProperty("N")]
        public string CommissionAsset { get; set; }

        [JsonProperty("T")]
        public long TradeTime { get; set; }

        [JsonProperty("t")]
        public int TradeId { get; set; }

        [JsonProperty("m")]
        public bool BuyerIsMaker { get; set; }

        //Don't know what is it. No documentation
        [JsonProperty("Q")]
        public string Q { get; set; }

        //Don't know what is it. No documentation
        [JsonProperty("O")]
        public string O { get; set; }

        //Don't know what is it. No documentation
        [JsonProperty("I")]
        public string I { get; set; }

        [JsonProperty("Z")]
        public decimal CumulativeQuoteAssetTransactedQuantity { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("S")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderSide Side { get; set; }

        [JsonProperty("o")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderType Type { get; set; }

        [JsonProperty("q")]
        public decimal OrderQuantity { get; set; }

        [JsonProperty("p")]
        public decimal Price { get; set; }

        [JsonProperty("X")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderStatus Status { get; set; }

        [JsonProperty("i")]
        public int OrderId { get; set; }

        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }
    }
}