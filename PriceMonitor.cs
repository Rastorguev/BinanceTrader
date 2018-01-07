using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Entities;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class PriceMonitor
    {
        private readonly BinanceApi _api;
        private readonly string _baseAsset;
        private readonly string _quoteAsset;
        private readonly string _interval;
        private readonly int _limit;

        public PriceMonitor(
            BinanceApi api,
            string baseAsset,
            string quoteAsset,
            string interval,
            int limit)
        {
            _api = api;
            _baseAsset = baseAsset;
            _quoteAsset = quoteAsset;
            _interval = interval;
            _limit = limit;
        }

        public PriceState State { get; set; }

        public void Start()
        {
            var chart =
                new CandlesChart
                {
                    Candles = _api.GetCandles(_baseAsset, _quoteAsset, _interval, _limit).Result
                        .Candles.OrderBy(c => c.OpenTime).ToList()
                };

            var analyzer = new ChartAnalyzer();
            var crossovers = analyzer.FindMACrossovers(chart, 7, 25);

            var initialBaseAmount = 1000m;
            var initialQuoteAmount = 0m;

            var baseAmount = 1000m;
            var quoteAmount = 0m;
            var price = 0.001m;
            var fluctuation = 1;

            List<DateTime> buyTime=new List<DateTime>();
            List<DateTime> sellTime=new List<DateTime>();

            foreach (var point in crossovers)
            {
                if (point.Type == MATrendType.BearishCrossover &&
                    baseAmount != 0
                    && point.Price > price + price.Percents(fluctuation)
                )
                {
                    price = point.Price;
                    quoteAmount = baseAmount * price;
                    baseAmount = 0;
                    sellTime.Add(point.Time);

                }
                else if (point.Type == MATrendType.BullishCrossover &&
                         quoteAmount != 0 &&
                         point.Price < price - price.Percents(fluctuation)
                )
                {
                    price = point.Price;
                    baseAmount = quoteAmount / price;
                    quoteAmount = 0;

                    buyTime.Add(point.Time);
                }
            }

            var baseProfit = baseAmount - initialBaseAmount;
            var quoteProfit = quoteAmount - initialQuoteAmount;
        }

        public class Stat
        {
            public DateTime Time { get; set; }
            public decimal MA7 { get; set; }
            public decimal MA25 { get; set; }
            public decimal Diff { get; set; }
        }

        private decimal CalculateAveragePrice([NotNull] List<Candle> candles, int n)
        {
            var range = candles.GetRange(candles.Count - n, n);
            return range.Average(c => c.ClosePrice);
        }

        public enum PriceState
        {
            Unknown,
            Rising,
            Falling,
            Changing
        }
    }
}