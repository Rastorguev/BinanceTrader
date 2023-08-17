using BinanceApi.Models.Enums;

namespace BinanceTrader.Core;

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