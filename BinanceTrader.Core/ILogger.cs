using System;
using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.WebSocket;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public interface ILogger
    {
        void LogOrder([NotNull] string eventName, [NotNull] IOrder order);
        void LogOrderRequest([NotNull] string eventName, [NotNull] OrderRequest orderRequest);
        void LogMessage([NotNull] string eventName, [NotNull] string message);
        void LogMessage([NotNull] string eventName, [NotNull] Dictionary<string, string> properties);
        void LogWarning([NotNull] string eventName, [NotNull] string message);
        void LogException([NotNull] Exception ex);
    }
}