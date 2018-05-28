using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class OrderRequest
    {
        public string Symbol { get; set; }
        public OrderSide Side { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }

        public OrderRequest(string symbol, OrderSide side, decimal qty, decimal price)
        {
            Symbol = symbol;
            Side = side;
            Qty = qty;
            Price = price;
        }
    }

    public static class OrderRequestExtensions
    {
        public static bool MeetsTradingRules([NotNull] this OrderRequest order, [NotNull] ITradingRules rules)
        {
            return
                order.Price >= rules.MinPrice &&
                order.Price <= rules.MaxPrice &&
                order.Price * order.Qty >= rules.MinNotional &&
                (order.Price - rules.MinQty) % rules.TickSize == 0 &&
                order.Qty >= rules.MinQty &&
                order.Qty <= rules.MaxQty &&
                (order.Qty - rules.MinQty) % rules.StepSize == 0;
        }
    }
}