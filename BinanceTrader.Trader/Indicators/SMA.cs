using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader.Indicators
{
    public static class SMA
    {
        [NotNull]
        public static List<decimal> Calculate([NotNull] List<decimal> values, int period)
        {
            var sma = new List<decimal>();

            for (var i = 0; i < values.Count; i++)
            {
                if (i == 0)
                {
                    sma.Add(values[i]);
                    continue;
                }

                var n = i < period ? i : period;

                var average = values.GetRange(i - n, n).Average().Round();
                sma.Add(average);
            }

            return sma;
        }
    }
}