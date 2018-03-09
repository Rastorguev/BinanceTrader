using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.Market;
using JetBrains.Annotations;

namespace BinanceTrader.TradeSessions
{
    public interface ITradeSession
    {
        [NotNull] ITradeAccount Run([NotNull] [ItemNotNull] List<Candlestick> candles);
    }
}