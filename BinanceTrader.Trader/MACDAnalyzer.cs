using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Entities;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public static class MACDAnalyzer
    {
        [NotNull]
        public static List<MACDItem> CalculateMACD(
            [NotNull] [ItemNotNull] this List<Candle> candles,
            int shortPeriod,
            int longPeriod,
            int signalPeriod)
        {
            var prices = candles.Select(c => (decimal?) c.ClosePrice).ToList();
            var shortSMAs = CalculateSMAs(prices, shortPeriod);
            var longSMAs = CalculateSMAs(prices, longPeriod);
            var shortEMAs = CalculateEMAs(prices, shortPeriod);
            var longEMAs = CalculateEMAs(prices, longPeriod);
            var macds = CalculateMACDs(shortEMAs, longEMAs);
            var signals = CalculateEMAs(macds, signalPeriod);

            return candles.Select((t, i) => new MACDItem
                {
                    Candle = candles[i],
                    ShortSMA = shortSMAs[i],
                    ShortEMA = shortEMAs[i],
                    LongSMA = longSMAs[i],
                    LongEMA = longEMAs[i],
                    MACD = macds[i],
                    Signal = signals[i]
                })
                .ToList();
        }

        [NotNull]
        private static List<decimal?> CalculateMACDs(
            [NotNull] List<decimal?> shortEMAs,
            [NotNull] List<decimal?> longEMAs)
        {
            var macd = new List<decimal?>();
            for (var i = 0; i < shortEMAs.Count; i++)
            {
                var shortEMA = shortEMAs[i];
                var longEMA = longEMAs[i];

                if (shortEMA == null || longEMA == null)
                {
                    macd.Add(null);
                }
                else
                {
                    macd.Add(shortEMA.Value - longEMA.Value);
                }
            }

            return macd;
        }

        [NotNull]
        private static List<decimal?> CalculateSMAs([NotNull] List<decimal?> values, int period)
        {
            var smas = new List<decimal?>();

            for (var i = 0; i < values.Count; i++)
            {
                if (i < period - 1 || values[i] == null)
                {
                    smas.Add(null);

                    continue;
                }

                var range = values.GetRange(i - period + 1, period);
                if (range.Any(v => v == null))
                {
                    smas.Add(null);

                    continue;
                }

                var sma = range.Average(p => p.Value).Round();
                smas.Add(sma);
            }

            return smas;
        }

        [NotNull]
        private static List<decimal?> CalculateEMAs([NotNull] List<decimal?> values, int period)
        {
            var emas = new List<decimal?>();
            var k = 2 / (decimal) (period + 1);

            for (var i = 0; i < values.Count; i++)
            {
                if (i == 0)
                {
                    emas.Add(values[i]);

                    continue;
                }

                var current = values[i];
                var prev = emas[i - 1];

                if (current == null || prev == null)
                {
                    emas.Add(null);

                    continue;
                }

                //EMA = Price(t) * k + EMA(y) * (1 – k)
                //t = today, y = yesterday, N = number of days in EMA, k = 2 / (N + 1)
                var ema = (current.Value * k + emas[i - 1].Value * (1 - k)).Round();
                emas.Add(ema);
            }

            return emas;
        }

        [NotNull]
        [ItemNotNull]
        public static List<MACDItem> GetCrossovers([NotNull] [ItemNotNull] List<MACDItem> items)
        {
            var crossovers = new List<MACDItem>();
            for (var i = 0; i < items.Count; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                var current = items[i];
                var prev = items[i - 1];

                if (current.GetMACDHistType() == MACDHistType.Positive &&
                    prev.GetMACDHistType() == MACDHistType.Negative ||
                    current.GetMACDHistType() == MACDHistType.Negative &&
                    prev.GetMACDHistType() == MACDHistType.Positive)
                {
                    crossovers.Add(current);
                }
            }

            return crossovers;
        }
    }
}

public enum MACDHistType
{
    Undefined,
    Positive,
    Negative,
    Neutral
}

public enum TradeAction
{
    Ignore,
    Buy,
    Sell
}