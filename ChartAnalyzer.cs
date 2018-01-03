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
        public void FindMACrossovers([NotNull] CandlesChart chart, int shortMAPeriod, int longMAPeriod)
        {
            var shortMA = CalculateMA(chart, shortMAPeriod);
            var longMA = CalculateMA(chart, longMAPeriod);

            var diffs = new List<MAPoint>();

            foreach (var longPoint in longMA)
            {
                var shortPoint = shortMA.First(p => p.Time == longPoint.Time).NotNull();
                diffs.Add(new MAPoint
                {
                    Time = longPoint.Time,
                    Price = shortPoint.Price - longPoint.Price
                });
            }

            var crossPoints = new List<MAPoint>();

            for (var i = 1; i < diffs.Count - 1; i++)
            {
                var prev = diffs[i - 1].NotNull();
                var next = diffs[i + 1].NotNull();

                if (prev.Price < 0 && next.Price > 0 ||
                    prev.Price > 0 && next.Price < 0)
                {
                    crossPoints.Add(diffs[i]);
                }
            }

            var startTime = longMA.First().NotNull().Time;
            var endTime = longMA.Last().NotNull().Time;

            var orderedShort = shortMA.OrderBy(ma => ma.Price).ToList();
            var minShort = shortMA.OrderBy(ma => ma.Price).First();
            var maxShort = shortMA.OrderBy(ma => ma.Price).Last();

            var orderedLong = longMA.OrderBy(ma => ma.Price).ToList();
            var minLong = longMA.OrderBy(ma => ma.Price).First();
            var maxLong = longMA.OrderBy(ma => ma.Price).Last();
        }

        [NotNull]
        [ItemNotNull]
        private List<MAPoint> CalculateMA([NotNull] CandlesChart chart, int period)
        {
            var points = new List<MAPoint>();
            var candles = chart.Candles;
            for (var i = period; i < candles.Count; i++)
            {
                var start = i - period;
                var range = candles.GetRange(start, period);
                var average = range.Average(c => c.ClosePrice);

                points.Add(new MAPoint
                {
                    Price = average.RoundPrice(),
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
}