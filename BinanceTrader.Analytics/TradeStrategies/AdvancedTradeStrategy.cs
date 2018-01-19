using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Utils;

namespace BinanceTrader.TradeStrategies
{
    //public class AdvancedTradeStrategy : ITradeStrategy
    //{
    //    public TradeAction GetTradeAction(List<MACDItem> macd)
    //    {
    //        if (macd.Count < 3)
    //        {
    //            return TradeAction.Ignore;
    //        }

    //        var current = macd.Last().NotNull();
    //        var prev = macd[macd.Count - 3];
    //        var prev2 = macd.GetRange(macd.Count - 2, 1);

    //        if (current.GetMACDHistType() == MACDHistType.Positive &&
    //            prev.GetMACDHistType() == MACDHistType.Negative &&
    //            prev2.Count(i => i.NotNull().GetMACDHistType() == MACDHistType.Positive) == 1)
    //        {
    //            return TradeAction.Buy;
    //        }

    //        if (current.GetMACDHistType() == MACDHistType.Negative &&
    //            prev.GetMACDHistType() == MACDHistType.Positive &&
    //            prev2.Count(i => i.NotNull().GetMACDHistType() == MACDHistType.Negative) == 1)
    //        {
    //            return TradeAction.Sell;
    //        }

    //        return TradeAction.Ignore;
    //    }
    //}
}