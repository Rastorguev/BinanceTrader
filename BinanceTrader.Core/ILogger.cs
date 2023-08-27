using BinanceApi.Models.WebSocket;

namespace BinanceTrader.Core;

public interface ILogger
{
    void LogOrderPlaced(IOrder order);
    void LogOrderCompleted(IOrder order);
    void LogOrderCanceled(IOrder order);
    void LogOrderRequest(string eventName, OrderRequest orderRequest);
    void LogMessage(string eventName, string message);
    void LogMessage(string eventName, Dictionary<string, string> properties);
    void LogException(Exception ex);
}