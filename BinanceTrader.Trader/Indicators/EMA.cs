using System.Collections.Generic;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader.Indicators
{
    public static class EMA
    {
        [NotNull]
        public static List<decimal> Calculate([NotNull] List<decimal> prices, int period)
        {
            var ema = new List<decimal>();
            var k = 2 / (decimal) (period + 1);

            for (var i = 0; i < prices.Count; i++)
            {
                if (i == 0)
                {
                    ema.Add(prices[i]);

                    continue;
                }

                var current = prices[i];
                var prev = ema[i - 1];

                //EMA = Price(t) * k + EMA(y) * (1 – k)
                //t = today, y = yesterday, N = number of days in EMA, k = 2 / (N + 1)
                var average = (current * k + ema[i - 1] * (1 - k)).Round();
                ema.Add(average);
            }

            return ema;
        }
    }
}