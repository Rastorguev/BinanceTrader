using Binance.API.Csharp.Client.Models.WebSocket;
using JetBrains.Annotations;

namespace BinanceTrader.Trader;

public interface ILogger
{
    void LogOrderPlaced([NotNull] IOrder order);
    void LogOrderCompleted([NotNull] IOrder order);
    void LogOrderCanceled([NotNull] IOrder order);
    void LogOrderRequest([NotNull] string eventName, [NotNull] OrderRequest orderRequest);
    void LogMessage([NotNull] string eventName, [NotNull] string message);
    void LogMessage([NotNull] string eventName, [NotNull] Dictionary<string, string> properties);
    void LogException([NotNull] Exception ex);
}