using System;
using Binance.API.Csharp.Client.Models.WebSocket;

namespace BinanceTrader.Trader
{
    public interface ILogger
    {
        void LogOrder(string orderEvent, IOrder order, string info = null);
        void Log(Exception ex);
        void Log(string message);
        void LogImportant(string message);
        void LogTitle(string title);
        void LogSeparator();
    }
}