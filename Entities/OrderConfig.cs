using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Entities.Enums;

namespace BinanceTrader.Entities
{
    public class OrderConfig
    {
        public string BaseCurrency { get; set; }

        public string QuoteCurrency { get; set; }

        public OrderSide Side { get; set; }

        public OrderType Type { get; set; }

        public TimeInForceType TimeInForce { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }
    }
}