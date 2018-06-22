using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.Market;
using JetBrains.Annotations;

namespace BinanceTrader.Strategies
{
    public interface ITradingStrategy
    {
        TradeAction GetTradeAction([ItemNotNull] [NotNull] IReadOnlyList<Candlestick> candles, int index);
    }

    public enum TradeAction
    {
        Ignore,
        Buy,
        Sell
    }
}