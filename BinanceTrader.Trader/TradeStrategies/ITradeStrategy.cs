using System.Collections.Generic;
using BinanceTrader.Entities;
using JetBrains.Annotations;

namespace BinanceTrader.TradeStrategies
{
    public interface ITradeStrategy
    {
        TradeActionType GetTradeAction([NotNull] List<Candle> candles);
    }
}