using System;
using Binance.API.Csharp.Client.Models.WebSocket;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public interface ILogger
    {
        void LogOrder([NotNull] string orderEvent, [NotNull] IOrder order);
        void LogMessage([NotNull] string key, [NotNull] string message);
        void LogWarning([NotNull] string key, [NotNull] string message);
        void LogException([NotNull] Exception ex);
    }
}