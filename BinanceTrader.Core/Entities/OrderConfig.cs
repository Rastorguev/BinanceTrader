using BinanceTrader.Core.Entities.Enums;

namespace BinanceTrader.Core.Entities
{
    public class OrderConfig
    {
        public string BaseAsset { get; set; }

        public string QuoteAsset { get; set; }

        public OrderSide Side { get; set; }

        public OrderType Type { get; set; }

        public TimeInForceType TimeInForce { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }
    }
}