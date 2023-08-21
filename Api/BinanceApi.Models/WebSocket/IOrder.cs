using BinanceApi.Models.Enums;

namespace BinanceApi.Models.WebSocket
{
    public interface IOrder
    {
        int OrderId { get; set; }
        string Symbol { get; set; }
        OrderSide Side { get; set; }
        OrderType Type { get; set; }
        decimal OrderQuantity { get; set; }
        decimal Price { get; set; }
        OrderStatus Status { get; set; }
    }
}