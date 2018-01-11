using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Utils;

namespace BinanceTrader.TradeStrategies
{
    public class EMATradeStrategy : ITradeStrategy
    {
        public TradeAction DefineTradeAction(List<MACDItem> macd)
        {
            if (macd.Count < 3)
            {
                return TradeAction.Ignore;
            }

            var current = macd.Last().NotNull();

            var prev = macd[macd.Count - 2].NotNull();
            var prevPrev = macd[macd.Count - 3].NotNull();

            if (current.ShortEMA - current.LongEMA > 0 &&
                prev.ShortEMA - prev.LongEMA <= 0 &&
                prevPrev.ShortEMA - prevPrev.LongEMA < 0)
            {
                return TradeAction.Buy;
            }
            if (current.ShortEMA - current.LongEMA < 0 &&
                prev.ShortEMA - prev.LongEMA >= 0 &&
                prevPrev.ShortEMA - prevPrev.LongEMA > 0)
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}