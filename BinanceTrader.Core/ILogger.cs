using System;
using Binance.API.Csharp.Client.Models.WebSocket;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public interface ILogger
    {
        void LogOrder([NotNull] string eventName, [NotNull] IOrder order);
        void LogOrderRequest([NotNull] string eventName, [NotNull] OrderRequest orderRequest);
        void LogMessage([NotNull] string key, [NotNull] string message);
        void LogWarning([NotNull] string key, [NotNull] string message);
        void LogException([NotNull] Exception ex);
    }
}