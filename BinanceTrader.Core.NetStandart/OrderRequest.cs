using Binance.API.Csharp.Client.Models.Enums;

namespace BinanceTrader.Trader;

public class OrderRequest
{
    public OrderRequest(string symbol, OrderSide side, decimal qty, decimal price)
    {
        Symbol = symbol;
        Side = side;
        Qty = qty;
        Price = price;
    }

    public string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public decimal Qty { get; set; }
    public decimal Price { get; set; }
}