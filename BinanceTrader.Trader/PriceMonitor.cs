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

        public PriceMonitor(
            [NotNull] BinanceApi api
        )
        {
            _api = api;
        }

        public void Start()
        {
            var now = DateTime.Now;
            var candles = LoadCandles("XVG", "ETH", now.AddHours(-10), now, CandlesInterval.Minutes30);
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

                if (prev != null && prev.Type == MATrendType.BearishCrossover)
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
        private List<Candle> LoadCandles(string baseAsset, string quoteAsset, DateTime start, DateTime end,
            CandlesInterval interval)
        {
            const int maxRange = 500;
            var candles = new List<Candle>();

            while (start < end)
            {
                var intervalMinutes = maxRange * interval.ToMinutes();
                var rangeEnd = (end - start).TotalMinutes > intervalMinutes
                    ? start.AddMinutes(intervalMinutes)
                    : end;

                var rangeCandles = _api.GetCandles(baseAsset, quoteAsset, interval, start, rangeEnd).NotNull()
                    .Result.NotNull()
                    .ToList();

                candles.AddRange(rangeCandles);
                start = rangeEnd;
            }

            return candles.OrderBy(c => c.NotNull().OpenTime).ToList();
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
    }
}