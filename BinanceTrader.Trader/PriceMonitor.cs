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

        //public PriceState State { get; set; }

        public void Start()
        {
            var candles = LoadCandles();
            var trends = candles.DefineMATrends(3, 12);

            var crossovers = trends.Where(t =>
                t.NotNull().Type == MATrendType.BearishCrossover ||
                t.Type == MATrendType.BullishCrossover
            ).ToList();

            const decimal fluctuation = 0.2m;

            var ta = new MockTradingAccount(0, 1, 0, 0.1m);
            var minQuoteAmount = 0.01m;

            for (var i = 0; i < trends.Count; i++)
            {
                var point = trends[i];
                var prev = i - 1 > 0 ? trends[i - 1] : null;

                if (prev!=null && prev.Type == MATrendType.BearishCrossover)
                {
                    var baseAmount = Math.Floor(ta.CurrentBaseAmount);

                    if (baseAmount > 0 &&
                        point.Price > ta.LastPrice + ta.LastPrice.Percents(fluctuation))
                    {
                        ta.Sell(baseAmount, point.Price);
                        LogOrder("Sell", ta, point);
                    }
                }
                else if (prev != null && prev.Type == MATrendType.BullishCrossover)
                {
                    if (ta.CurrentQuoteAmount > minQuoteAmount &&
                        (ta.LastPrice == 0 ||
                         point.Price < ta.LastPrice - ta.LastPrice.Percents(fluctuation)))
                    {
                        var baseAmount = Math.Floor(ta.CurrentQuoteAmount / point.Price);
                        if (baseAmount > 0)
                        {
                            ta.Buy(baseAmount, point.Price);
                            LogOrder("Buy", ta, point);
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

        [NotNull]
        private List<Candle> LoadCandles()
        {
            var range = 500;
            var now = DateTime.Now;
            var start = now.AddHours(-5);
            var end = start.AddMinutes(range);
            var candles = new List<Candle>();

            while (start < now)
            {
                end = start.AddMinutes(range) <= now
                    ? start.AddMinutes(range)
                    : end.AddMinutes((now - start).TotalMinutes);

                var cndls = _api.GetCandles(_baseAsset, _quoteAsset, _interval, start, end).NotNull().Result.NotNull()
                    .ToList();

                start = start.AddMinutes(range);

                candles.AddRange(cndls);
            }

            return candles.OrderBy(c => c.OpenTime).ToList();
        }

        private static void LogOrder(string action, [NotNull] ITradingAccount ta, [NotNull] MATrend point)
        {
            var ca = ta.CurrentBaseAmount * ta.LastPrice + ta.CurrentQuoteAmount;

            Console.WriteLine(action);
            Console.WriteLine($"Trend :{point.Type}");
            Console.WriteLine(point.OpenTime);
            Console.WriteLine($"Price: {ta.LastPrice}");
            Console.WriteLine($"Base amount: {ta.CurrentBaseAmount}");
            Console.WriteLine($"Total: {ca.Round()}");
            Console.WriteLine();
        }

        //public enum PriceState
        //{
        //    Unknown,
        //    Rising,
        //    Falling,
        //    Changing
        //}
    }
}