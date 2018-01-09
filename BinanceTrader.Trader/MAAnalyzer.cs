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
            var longMA = CalculateMA(candles, longPeriod);
            var shortMA = CalculateMA(candles, shortPeriod);
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
                    ShortMAPrice = shortMAPoint.Price,
                    LongMAPrice = longMAPoint.Price
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
        private static List<MAPoint> CalculateMA([NotNull] [ItemNotNull] List<Candle> candles, int period)
        {
            var points = new List<MAPoint>();
            for (var i = period; i < candles.Count; i++)
            {
                var range = candles.GetRange(i - period, period);
                var average = range.Average(c => c.NotNull().ClosePrice);

                points.Add(new MAPoint(candles[i - 1].OpenTime, average.Round()));
            }

            return points;
        }

        private class MAPoint
        {
            public DateTime OpenTime { get; }
            public decimal Price { get; }

            public MAPoint(DateTime openTime, decimal price)
            {
                OpenTime = openTime;
                Price = price;
            }
        }
    }

    public class MATrend
    {
        public DateTime OpenTime { get; set; }
        public decimal Price { get; set; }
        public decimal ShortMAPrice { get; set; }
        public decimal LongMAPrice { get; set; }
        public MATrendType Type { get; set; }
        public decimal TrendStrength => (ShortMAPrice - LongMAPrice) * 100 / ShortMAPrice;
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