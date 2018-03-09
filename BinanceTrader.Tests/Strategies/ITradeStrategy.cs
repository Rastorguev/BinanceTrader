using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.Market;
using JetBrains.Annotations;

namespace BinanceTrader.Strategies
{
    public interface ITradeStrategy
    {
        TradeAction GetTradeAction([NotNull] List<Candlestick> candles);
    }

    public enum TradeAction
    {
        Ignore,
        Buy,
        Sell
    }
}