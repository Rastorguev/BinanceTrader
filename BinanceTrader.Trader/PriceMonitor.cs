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
        [NotNull] private readonly BinanceApi _api;
        private readonly string _baseAsset;
        private readonly string _quoteAsset;
        private readonly string _interval;
        private readonly int _limit;

        public PriceMonitor(
            [NotNull] BinanceApi api,
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
            var range = 500;
            var now = DateTime.Now;
            var start = now.AddDays(-5);
            var end = start.AddMinutes(range);
            var candles = new List<Candle>();

            while (start < now)
            {
                end = start.AddMinutes(range) <= now
                    ? start.AddMinutes(range)
                    : end.AddMinutes((now - start).TotalMinutes);

                var cndls = _api.GetCandles(_baseAsset, _quoteAsset, _interval, start, end).NotNull().Result.NotNull().Candles.ToList();

                start = start.AddMinutes(range);

                candles.AddRange(cndls);
            }

            var chart =
                new CandlesChart
                {
                    Candles = candles.OrderBy(c => c.OpenTime).ToList()

                    //Candles = _api.GetCandles(_baseAsset, _quoteAsset, _interval, now.AddMinutes(-10), now).Result
                    //    .Candles.OrderBy(c => c.OpenTime).ToList()
                };

            var analyzer = new ChartAnalyzer();
            var crossovers = analyzer.FindMACrossovers(chart, 3, 12);

            const decimal fluctuation = 0.5m;

            var buyTime = new List<DateTime>();
            var sellTime = new List<DateTime>();

            var ta = new MockTradingAccount(0, 1, 0, 0.1m);
            var minQuoteAmount = 0.01m;

            foreach (var point in crossovers)
            {
                if (point.Type == MATrendType.BearishCrossover)
                {
                    var baseAmount = Math.Floor(ta.CurrentBaseAmount);

                    if (baseAmount > 0 &&
                        point.Price > ta.LastPrice + ta.LastPrice.Percents(fluctuation))
                    {
                        ta.Sell(baseAmount, point.Price);
                        sellTime.Add(point.Time);

                        LogOrder(ta, point);
                    }
                }
                else if (point.Type == MATrendType.BullishCrossover)
                {
                    if (ta.CurrentQuoteAmount > minQuoteAmount &&
                        (ta.LastPrice == 0 ||
                         point.Price < ta.LastPrice - ta.LastPrice.Percents(fluctuation)))
                    {
                        var baseAmount = Math.Floor(ta.CurrentQuoteAmount / point.Price);
                        if (baseAmount > 0)
                        {
                            ta.Buy(baseAmount, point.Price);
                            buyTime.Add(point.Time);

                            LogOrder(ta, point);
                        }
                    }
                }
            }

            var initialAmount = ta.InitialBaseAmount * ta.InitialPrice + ta.InitialQuoteAmount;
            var currentAmount = ta.CurrentBaseAmount * ta.LastPrice + ta.CurrentQuoteAmount;
            var profit = MathUtils.CalculateProfit(
                initialAmount,
                currentAmount).Round();

            Console.WriteLine($"Profit {profit}");
        }

        private static void LogOrder(ITradingAccount ta, MATrendPoint point)
        {
            var ca = ta.CurrentBaseAmount * ta.LastPrice + ta.CurrentQuoteAmount;
            Console.WriteLine(point.Type);
            Console.WriteLine(point.Time);
            Console.WriteLine($"Price: {ta.LastPrice}");
            Console.WriteLine($"Base amount: {ta.CurrentBaseAmount}");
            Console.WriteLine($"Total: {ca.Round()}");
            Console.WriteLine();
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