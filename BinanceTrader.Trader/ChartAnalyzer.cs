using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Entities;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class ChartAnalyzer
    {
        public List<MATrendPoint> FindMACrossovers([NotNull] CandlesChart chart, int shortMAPeriod, int longMAPeriod)
        {
            var longMA = CalculateMA(chart, longMAPeriod);
            var shortMA = CalculateMA(chart, shortMAPeriod);
            shortMA = shortMA.GetRange(longMAPeriod - shortMAPeriod, longMA.Count);
            var candles = chart.Candles.GetRange(longMAPeriod - 1, longMA.Count);

            var trendPoints = new List<MATrendPoint>();

            for (var i = 0; i < longMA.Count; i++)
            {
                var longPoint = longMA[i];
                var shortPoint = shortMA[i];

                var current = new MATrendPoint
                {
                    Time = candles[i].OpenTime,
                    Price = candles[i].OpenPrice,
                    ShortMAPrice = shortPoint.Price,
                    LongMAPrice = longPoint.Price
                };

                if (current.TrendStrength > 0)
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

                trendPoints.Add(current);
            }

            SetPointsType(trendPoints);

            var crossovers = trendPoints.Where(p =>
                p.Type == MATrendType.BullishCrossover || p.Type == MATrendType.BearishCrossover).ToList();

            var startTime = longMA.First().NotNull().Time;
            var endTime = longMA.Last().NotNull().Time;

            var orderedShort = shortMA.OrderBy(ma => ma.Price).ToList();
            var minShort = shortMA.OrderBy(ma => ma.Price).First();
            var maxShort = shortMA.OrderBy(ma => ma.Price).Last();

            var orderedLong = longMA.OrderBy(ma => ma.Price).ToList();
            var minLong = longMA.OrderBy(ma => ma.Price).First();
            var maxLong = longMA.OrderBy(ma => ma.Price).Last();

            return crossovers;
        }

        private static void SetPointsType(IReadOnlyList<MATrendPoint> trendPoints)
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
                    current.TrendStrength > 0)
                {
                    current.Type = MATrendType.BullishCrossover;
                }
                else if (prev != null &&
                         next != null &&
                         prev.TrendStrength > 0 &&
                         next.TrendStrength < 0 &&
                         current.TrendStrength < 0)
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
        private static List<MAPoint> CalculateMA([NotNull] CandlesChart chart, int period)
        {
            var points = new List<MAPoint>();
            var candles = chart.Candles;

            for (var i = period; i < candles.Count; i++)
            {
                var range = candles.GetRange(i - period, period);
                var average = range.Average(c => c.ClosePrice);

                points.Add(new MAPoint
                {
                    Price = average.Round(),
                    Time = candles[i - 1].OpenTime
                });
            }

            return points;
        }
    }

    public class MAPoint
    {
        public DateTime Time { get; set; }
        public decimal Price { get; set; }
    }

    public class MATrendPoint
    {
        public DateTime Time { get; set; }
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