﻿using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Core.Entities.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BinanceTrader.Core.Entities
{
    public class Orders : List<Order>
    {
        public Order GetOrder(long id)
        {
            return this.FirstOrDefault(o => o.OrderId == id);
        }
    }

    public class Order
    {
        public string Symbol { get; set; }

        public long OrderId { get; set; }

        public string ClientOrderId { get; set; }

        public decimal OrigQty { get; set; }

        public decimal ExecutedQty { get; set; }

        public decimal Price { get; set; }

        public decimal StopPrice { get; set; }

        public decimal IcebergQty { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrderStatus Status { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TimeInForceType TimeInForce { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrderType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrderSide Side { get; set; }
    }
}