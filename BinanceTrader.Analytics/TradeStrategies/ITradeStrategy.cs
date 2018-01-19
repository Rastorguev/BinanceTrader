using System.Collections.Generic;
using BinanceTrader.Core.Entities;
using BinanceTrader.Core.Entities.Enums;
using JetBrains.Annotations;

namespace BinanceTrader.TradeStrategies
{
    public interface ITradeStrategy
    {
        TradeActionType GetTradeAction([NotNull] List<Candle> candles);
    }
}