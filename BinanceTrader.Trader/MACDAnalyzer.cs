using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Entities;
using BinanceTrader.Indicators;
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
            var prices = candles.Select(c =>  c.ClosePrice).ToList();
            var shortSMA = SMA.Calculate(prices, shortPeriod);
            var longSMA = SMA.Calculate(prices, longPeriod);
            var shortEMA = EMA.Calculate(prices, shortPeriod);
            var longEMA= EMA.Calculate(prices, longPeriod);
            var macds = MACD.Calculate(shortEMA, longEMA);
            var signal = EMA.Calculate(macds, signalPeriod);

            return candles.Select((t, i) => new MACDItem
                {
                    Candle = candles[i],
                    ShortSMA = shortSMA[i],
                    ShortEMA = shortEMA[i],
                    LongSMA = longSMA[i],
                    LongEMA = longEMA[i],
                    MACD = macds[i],
                    Signal = signal[i]
                })
                .ToList();
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