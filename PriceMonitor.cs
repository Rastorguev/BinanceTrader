using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Entities;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class PriceMonitor
    {
        private readonly BinanceApi _api;
        private readonly string _baseAsset;
        private readonly string _quoteAsset;

        public PriceMonitor(
            BinanceApi api,
            string baseAsset,
            string quoteAsset)
        {
            _api = api;
            _baseAsset = baseAsset;
            _quoteAsset = quoteAsset;
        }

        public PriceState State { get; set; }

        public void Start()
        {
            var chart =
                new CandlesChart
                {
                    Candles = _api.GetCandles(_baseAsset, _quoteAsset, "15m").Result
                        .Candles.OrderBy(c => c.OpenTime).ToList()
                };

            var analyzer = new ChartAnalyzer();
            analyzer.FindMACrossovers(chart, 7, 25);
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