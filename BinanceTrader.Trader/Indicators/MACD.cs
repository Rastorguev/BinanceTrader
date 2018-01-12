using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.Indicators
{
    public static class MACD
    {
        [NotNull]
        public static List<decimal> Calculate(
            [NotNull] List<decimal> shortEMAs,
            [NotNull] List<decimal> longEMAs)
        {
            var macd = new List<decimal>();
            for (var i = 0; i < shortEMAs.Count; i++)
            {
                var shortEMA = shortEMAs[i];
                var longEMA = longEMAs[i];

                macd.Add(shortEMA - longEMA);
            }

            return macd;
        }
    }
}