using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Utils;

namespace BinanceTrader.TradeStrategies
{
    public class AdvancedTradeStrategy : ITradeStrategy
    {
        public TradeAction DefineTradeAction(List<MACDItem> macd)
        {
            if (macd.Count < 4)
            {
                return TradeAction.Ignore;
            }

            var current = macd.Last().NotNull();
            var prev = macd[macd.Count - 4];
            var prev2 = macd.GetRange(macd.Count - 3, 2);

            if (current.GetMACDHistType() == MACDHistType.Positive &&
                prev.GetMACDHistType() == MACDHistType.Negative &&
                prev2.Count(i => i.NotNull().GetMACDHistType() == MACDHistType.Positive) == 2)
            {
                return TradeAction.Buy;
            }

            if (current.GetMACDHistType() == MACDHistType.Negative &&
                prev.GetMACDHistType() == MACDHistType.Positive &&
                prev2.Count(i => i.NotNull().GetMACDHistType() == MACDHistType.Negative) == 2)
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}