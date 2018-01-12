using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.TradeStrategies
{
    public interface ITradeStrategy
    {
        TradeAction GetTradeAction([NotNull] List<decimal> prices);
    }
}