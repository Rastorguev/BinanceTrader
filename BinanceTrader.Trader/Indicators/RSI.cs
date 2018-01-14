using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BinanceTrader.Indicators
{
    public static class RSI
    {
        [NotNull]
        public static List<decimal> Calculate([NotNull] List<decimal> prices, int period)
        {

            var rsi = new decimal[prices.Count];
            var gain = 0.0m;
            var loss = 0.0m;

            if (prices.Count < period)
            {
                return rsi.ToList();
            }

            // first RSI value
            rsi[0] = 0m;
            for (var i = 1; i < period; i++)
            {
                var diff = prices[i] - prices[i - 1];
                if (diff >= 0)
                {
                    gain += diff;
                }
                else
                {
                    loss -= diff;
                }
            }

            var avrGain = gain / period;
            var avrLoss = loss / period;
            var rs = gain / loss;
            rsi[period - 1] = 100 - (100 / (1 + rs));

            for (var i = period; i < prices.Count; i++)
            {
                var diff = prices[i] - prices[i - 1];

                if (diff >= 0)
                {
                    avrGain = ((avrGain * (period - 1)) + diff) / period;
                    avrLoss = (avrLoss * (period - 1)) / period;
                }
                else
                {
                    avrLoss = ((avrLoss * (period - 1)) - diff) / period;
                    avrGain = (avrGain * (period - 1)) / period;
                }

                rs = avrGain / avrLoss;

                rsi[i] = 100 - (100 / (1 + rs));
            }

            return rsi.ToList();
        }

        //[NotNull]
        //public static List<decimal> Calculate([NotNull] List<decimal> prices, int period)
        //{
        //    var rsi = new decimal[prices.Count];
        //    if (prices.Count < period)
        //    {
        //        return rsi.ToList();
        //    }
        //    var avgGains = new decimal[prices.Count];
        //    var avgLoss = new decimal[prices.Count];

        //    for (var i = 0; i < prices.Count; i++)
        //    {
        //        if (i < period)
        //        {
        //            rsi[i] = 0;
        //            continue;
        //        }

        //        var gain = 0m;
        //        var loss = 0m;

        //        for (var j = i - period; j < i; j++)
        //        {
        //            var diff = prices[j + 1] - prices[j];
        //            if (diff >= 0)
        //            {
        //                gain += diff;
        //            }
        //            else
        //            {
        //                loss += Math.Abs(diff);
        //            }
        //        }

        //        avgGains[i] = gain;
        //        avgLoss[i] = loss;
        //    }

        //    for (var i = 1; i < prices.Count; i++)
        //    {
        //        var avrGain = (avgGains[i - 1] * (period - 1) + avgGains[i]) / period;
        //        var avrLoss = (avgLoss[i - 1] * (period - 1) + avgLoss[i]) / period;

        //        if (avrLoss == 0)
        //        {
        //            rsi[i] = 100;
        //            continue;
        //        }

        //        var rs = avrGain / avrLoss;

        //        rsi[i] = 100 - 100 / (1 + rs);
        //    }

        //    return rsi.ToList();
        //}
    }
}