﻿using BinanceApi.Models.Enums;
using BinanceApi.Models.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BinanceApi.Models.Account
{
    public class NewOrder : IOrder
    {
        [JsonProperty("executedQty")]
        public decimal ExecutedQty { get; set; }

        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; }

        [JsonProperty("transactTime")]
        public long TransactTime { get; set; }

        [JsonProperty("orderId")]
        public int OrderId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderSide Side { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderType Type { get; set; }

        [JsonProperty("origQty")]
        public decimal OrderQuantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderStatus Status { get; set; }
    }
}