using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Entities;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public static class MAAnalyzer
    {
        [NotNull]
        public static List<MATrend> DefineMATrends(
            [NotNull] [ItemNotNull] this List<Candle> candles,
            int shortPeriod,
            int longPeriod,
            int signalPeriod)
        {
            var prices = candles.Select(c => (decimal?) c.ClosePrice).ToList();
            var shortSMAs = CalculateSMAs(prices, shortPeriod);
            var longSMAs = CalculateSMAs(prices, longPeriod);
            var shortEMAs = CalculateEMAs(prices, shortSMAs, shortPeriod);
            var longEMAs = CalculateEMAs(prices, longSMAs, longPeriod);
            var macds = CalculateMACDs(shortEMAs, longEMAs);
            var signals = CalculateSMAs(macds, signalPeriod);

            //var signal = CalculateSMAs(macd, signalPeriod);
            var trendPoints = new List<MATrend>();

            for (var i = 0; i < candles.Count; i++)
            {
                trendPoints.Add(new MATrend
                {
                    OpenTime = candles[i].NotNull().OpenTime,
                    Price = candles[i].NotNull().OpenPrice,
                    ShortSMA = shortSMAs[i],
                    ShortEMA = shortEMAs[i],
                    LongSMA = longSMAs[i],
                    LongEMA = longEMAs[i],
                    MACD = macds[i],
                    Signals = signals[i]
                });
            }

            //longMA = CalculateEMAs(longMA, longPeriod);
            //shortMA = CalculateEMAs(shortMA, shortPeriod);

            //shortMA = shortMA.GetRange(longPeriod - shortPeriod, longMA.Count);
            //var maCandles = candles.GetRange(longPeriod - 1, longMA.Count);
            //var trendPoints = new List<MATrend>();

            //for (var i = 0; i < longMA.Count; i++)
            //{
            //    var longMAPoint = longMA[i];
            //    var shortMAPoint = shortMA[i].NotNull();
            //    var current = new MATrend
            //    {
            //        OpenTime = maCandles[i].NotNull().OpenTime,
            //        Price = maCandles[i].NotNull().OpenPrice,
            //        ShortSMA = shortMAPoint.SMA,
            //        ShortEMA = shortMAPoint.EMA,
            //        LongSMA = longMAPoint.SMA,
            //        LongEMA = longMAPoint.EMA
            //    };

            //    trendPoints.Add(current);
            //}

            //SetPointsType(trendPoints);

            return trendPoints;
        }

        [NotNull]
        private static List<decimal?> CalculateMACDs([NotNull] List<decimal?> shortEMAs,
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

        //private static void SetPointsType([NotNull] [ItemNotNull] IReadOnlyList<MATrend> trendPoints)
        //{
        //    for (var i = 1; i < trendPoints.Count - 1; i++)
        //    {
        //        var current = trendPoints[i].NotNull();
        //        var prev = i > 0 ? trendPoints[i - 1].NotNull() : null;
        //        var next = i < trendPoints.Count - 1 ? trendPoints[i + 1] : null;

        //        if (prev != null &&
        //            next != null &&
        //            prev.TrendStrength < 0 &&
        //            next.TrendStrength > 0 &&
        //            current.TrendStrength >= 0)
        //        {
        //            current.Type = MATrendType.BullishCrossover;
        //        }
        //        else if (prev != null &&
        //                 next != null &&
        //                 prev.TrendStrength > 0 &&
        //                 next.TrendStrength < 0 &&
        //                 current.TrendStrength <= 0)
        //        {
        //            current.Type = MATrendType.BearishCrossover;
        //        }
        //        else if (current.TrendStrength > 0)
        //        {
        //            current.Type = MATrendType.Bullish;
        //        }
        //        else if (current.TrendStrength < 0)
        //        {
        //            current.Type = MATrendType.Bearish;
        //        }
        //        else if (current.TrendStrength == 0)
        //        {
        //            current.Type = MATrendType.Neutral;
        //        }
        //    }
        //}

        [NotNull]
        private static List<decimal?> CalculateSMAs([NotNull] List<decimal?> values, int period)
        {
            var smas = new List<decimal?>();

            for (var i = 0; i < values.Count; i++)
            {
                if (i < period - 1 || values[i] == null)
                {
                    smas.Add(null);
                }
                else
                {
                    var range = values.GetRange(i - period + 1, period);

                    if (range.Any(v => v == null))
                    {
                        smas.Add(null);
                    }
                    else
                    {
                        var sma = range.Average(p => p.Value).Round();

                        smas.Add(sma);
                    }
                }
            }

            return smas;
        }

        [NotNull]
        private static List<decimal?> CalculateEMAs(
            [NotNull] List<decimal?> values,
            [NotNull] List<decimal?> smas,
            int period)
        {
            var emas = new List<decimal?>();
            var k = 2 / ((decimal) (period + 1)).Round();

            for (var i = 0; i < values.Count; i++)
            {
                if (i < period - 1 || values[i] == null)
                {
                    emas.Add(null);
                }
                else
                {
                    if (i == period - 1)
                    {
                        emas.Add(smas[i]);
                    }
                    else
                    {
                        var prev = emas[i - 1].Value;

                        //EMA = Price(t) * k + EMA(y) * (1 – k)
                        //t = today, y = yesterday, N = number of days in EMA, k = 2 / (N + 1)
                        var ema = (values[i].Value * k + prev * (1 - k)).Round();

                        emas.Add(ema);
                    }
                }
            }

            return emas;
        }

        private class MAPoint
        {
            public DateTime OpenTime { get; set; }
            public decimal ClosePrice { get; set; }
            public decimal SMA { get; set; }
            public decimal EMA { get; set; }
        }
    }

    public class MATrend
    {
        public DateTime OpenTime { get; set; }
        public decimal Price { get; set; }
        public decimal? ShortSMA { get; set; }
        public decimal? ShortEMA { get; set; }
        public decimal? LongSMA { get; set; }
        public decimal? LongEMA { get; set; }

        public MATrendType Type { get; set; }
        //public decimal TrendStrength => (ShortSMA - LongSMA) * 100 / ShortSMA;

        public decimal? MACD { get; set; }
        public decimal? Signals { get; set; }

        public decimal? MACDGist
        {
            get
            {
                if (MACD != null && Signals != null)
                {
                    return MACD.Value - Signals.Value;
                }

                return null;
            }
        }

        //public decimal? MACD
        //{
        //    get
        //    {
        //        if (ShortEMA != null && LongEMA != null)
        //        {
        //            return ShortEMA.Value - LongEMA.Value;
        //        }

        //        return null;
        //    }
        //}
    }

    public enum MATrendType
    {
        Undefined,
        Neutral,
        Bullish,
        BullishCrossover,
        Bearish,
        BearishCrossover
    }
}