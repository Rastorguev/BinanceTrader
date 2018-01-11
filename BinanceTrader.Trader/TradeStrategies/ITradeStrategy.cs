using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.TradeStrategies
{
    public interface ITradeStrategy
    {
        TradeAction DefineTradeAction([NotNull] [ItemNotNull] List<MACDItem> macd);
    }
}