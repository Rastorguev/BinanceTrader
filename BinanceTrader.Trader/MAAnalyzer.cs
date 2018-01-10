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
            int longPeriod)
        {
            var longMA = CalculateSMA(candles, longPeriod);
            var shortMA = CalculateSMA(candles, shortPeriod);
            longMA = CalculateEMA(longMA, longPeriod);
            shortMA = CalculateEMA(shortMA, shortPeriod);

            shortMA = shortMA.GetRange(longPeriod - shortPeriod, longMA.Count);
            var maCandles = candles.GetRange(longPeriod - 1, longMA.Count);
            var trendPoints = new List<MATrend>();

            for (var i = 0; i < longMA.Count; i++)
            {
                var longMAPoint = longMA[i];
                var shortMAPoint = shortMA[i].NotNull();
                var current = new MATrend
                {
                    OpenTime = maCandles[i].NotNull().OpenTime,
                    Price = maCandles[i].NotNull().OpenPrice,
                    ShortSMA = shortMAPoint.SMA,
                    ShortEMA = shortMAPoint.EMA,
                    LongSMA = longMAPoint.SMA,
                    LongEMA = longMAPoint.EMA
                };

                trendPoints.Add(current);
            }



            SetPointsType(trendPoints);

            return trendPoints;
        }

        private static void SetPointsType([NotNull] [ItemNotNull] IReadOnlyList<MATrend> trendPoints)
        {
            for (var i = 1; i < trendPoints.Count - 1; i++)
            {
                var current = trendPoints[i].NotNull();
                var prev = i > 0 ? trendPoints[i - 1].NotNull() : null;
                var next = i < trendPoints.Count - 1 ? trendPoints[i + 1] : null;

                if (prev != null &&
                    next != null &&
                    prev.TrendStrength < 0 &&
                    next.TrendStrength > 0 &&
                    current.TrendStrength >= 0)
                {
                    current.Type = MATrendType.BullishCrossover;
                }
                else if (prev != null &&
                         next != null &&
                         prev.TrendStrength > 0 &&
                         next.TrendStrength < 0 &&
                         current.TrendStrength <= 0)
                {
                    current.Type = MATrendType.BearishCrossover;
                }
                else if (current.TrendStrength > 0)
                {
                    current.Type = MATrendType.Bullish;
                }
                else if (current.TrendStrength < 0)
                {
                    current.Type = MATrendType.Bearish;
                }
                else if (current.TrendStrength == 0)
                {
                    current.Type = MATrendType.Neutral;
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        private static List<MAPoint> CalculateSMA([NotNull] [ItemNotNull] List<Candle> candles, int period)
        {
            var points = new List<MAPoint>();
            for (var i = period; i < candles.Count; i++)
            {
                var range = candles.GetRange(i - period, period);
                var sma = range.Average(c => c.NotNull().ClosePrice);

                points.Add(new MAPoint
                {
                    OpenTime = candles[i - 1].OpenTime,
                    ClosePrice = candles[i - 1].ClosePrice,
                    SMA = sma.Round()
                });
            }

            return points;
        }

        [NotNull]
        [ItemNotNull]
        private static List<MAPoint> CalculateEMA([NotNull] [ItemNotNull] List<MAPoint> points, int period)
        {
            for (var i = 0; i < points.Count; i++)
            {
                var current = points[i];
                var k = 2 / ((decimal) (period + 1)).Round();

                if (i == 0)
                {
                    current.EMA = current.SMA;
                }
                else
                {
                    var prev = points[i - 1];

                    //EMA = Price(t) * k + EMA(y) * (1 – k)
                    //t = today, y = yesterday, N = number of days in EMA, k = 2 / (N + 1)
                    current.EMA = current.ClosePrice * k + prev.EMA * (1 - k);
                }
            }

            return points;
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
        public decimal ShortSMA { get; set; }
        public decimal ShortEMA { get; set; }
        public decimal LongSMA { get; set; }
        public decimal LongEMA { get; set; }
       
        public MATrendType Type { get; set; }
        public decimal TrendStrength => (ShortSMA - LongSMA) * 100 / ShortSMA;
        public decimal MACD => ShortEMA - LongEMA;
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