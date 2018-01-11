using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Utils;

namespace BinanceTrader.TradeStrategies
{
    public class BasicTradeStrategy : ITradeStrategy
    {
        public TradeAction DefineTradeAction(List<MACDItem> macd)
        {
            if (macd.Count < 2)
            {
                return TradeAction.Ignore;
            }

            var current = macd.Last().NotNull();
            var prev = macd[macd.Count - 2].NotNull();

            if (current.GetMACDHistType() == MACDHistType.Positive && prev.GetMACDHistType() == MACDHistType.Negative)
            {
                return TradeAction.Buy;
            }
            if (current.GetMACDHistType() == MACDHistType.Negative && prev.GetMACDHistType() == MACDHistType.Positive)
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}