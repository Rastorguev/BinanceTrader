using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BinanceTrader.Analytics.Indicators
{
    public static class MACD
    {
        [NotNull]
        public static List<decimal> Calculate(
            [NotNull] List<decimal> shortEMA,
            [NotNull] List<decimal> longEMA)
        {
            return shortEMA.Select((t, i) => t - longEMA[i]).ToList();
        }

        public static decimal GetHistValue(decimal macd, decimal signal)
        {
            return macd - signal;
        }
    }
}