using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader.Indicators
{
    public static class SMA
    {
        [NotNull]
        public static List<decimal> Calculate([NotNull] List<decimal> prices, int period)
        {
            var sma = new List<decimal>();

            for (var i = 0; i < prices.Count; i++)
            {
                if (i == 0)
                {
                    sma.Add(prices[i]);
                    continue;
                }

                var n = i < period ? i : period;

                var average = prices.GetRange(i - n, n).Average().Round();
                sma.Add(average);
            }

            return sma;
        }
    }
}