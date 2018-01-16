using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Utils;
using JetBrains.Annotations;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core;
using Trady.Core.Infrastructure;

namespace BinanceTrader
{
    public class PriceMonitor
    {
        [NotNull] private readonly BinanceApi _api;

        public PriceMonitor(
            [NotNull] BinanceApi api
        ) => _api = api;

        public void Start()
        {
            var assets = new List<string> {"TRX", "CND", "TNB", "POE", "FUN", "XVG", "MANA", "CDT", "LEND", "DNT"};

            var strategies =
                new List<(string Name, Predicate<IIndexedOhlcv> BuyRule, Predicate<IIndexedOhlcv> SellRule)>();

            const int shortPeriod = 7;
            const int longPeriod = 25;
            const int signalPeriod = 9;

            var macdProfit = 0m;
            var smaProfit = 0m;
            var advancedProfit = 0m;

            var macdStrategy = ("MACD",
                Rule.Create(ic => ic.IsMacdBullishCross(shortPeriod, longPeriod, signalPeriod))
                    .NotNull(),
                Rule.Create(ic => ic.IsMacdBearishCross(shortPeriod, longPeriod, signalPeriod))
                    .NotNull());

            var smaStrategy = ("SMA",
                Rule.Create(ic => ic.IsSmaBullishCross(shortPeriod, longPeriod))
                    .NotNull(),
                Rule.Create(ic => ic.IsSmaBearishCross(shortPeriod, longPeriod))
                    .NotNull());

            var advancedStrategy = ("Advanced",
                Rule.Create(ic => ic.IsStochRsiOversold(15))
                    //.Or(ic => ic.IsMacdBullishCross(shortPeriod, longPeriod, signalPeriod))
                    //.Or(ic => ic.IsSmaBullishCross(shortPeriod, longPeriod))
                    .NotNull(),
                Rule.Create(ic => ic.IsMacdBearishCross(shortPeriod, longPeriod, signalPeriod))

                //Rule.Create(ic => ic.IsStochRsiOverbought(15))
                //    .Or(ic => ic.IsMacdBearishCross(shortPeriod, longPeriod, signalPeriod))
                //    .Or(ic => ic.IsSmaBearishCross(shortPeriod, longPeriod))
                //    .NotNull()
                    
                    );

            strategies.Add(macdStrategy);
            strategies.Add(smaStrategy);
            strategies.Add(advancedStrategy);

            foreach (var asset in assets)
            {
                var candles = LoadCandles(
                    asset,
                    "ETH",
                    new DateTime(2018, 1, 16, 14, 0, 0),
                    new DateTime(2018, 1, 16, 16, 30, 0),
                    CandlesInterval.Minutes1);
                var tradyCandles = candles.ToTradyCandles();

                Console.WriteLine(asset);

                foreach (var strategy in strategies)
                {
                    var result = Trade(tradyCandles, strategy.BuyRule, strategy.SellRule);
                    var profit = result.GetProfit();

                    if (strategy.Equals(macdStrategy))
                    {
                        macdProfit += profit;
                    }
                    else if (strategy.Equals(smaStrategy))
                    {
                        smaProfit += profit;
                    }
                    else

                    if (strategy.Equals(advancedStrategy))
                    {
                        advancedProfit += profit;
                    }

                    Console.WriteLine($" {strategy.Name}: {profit}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("----------------------");
            Console.WriteLine($"MACD Avg: {macdProfit / assets.Count}");
            Console.WriteLine($"SMA Avg: {smaProfit / assets.Count}");
            Console.WriteLine($"Advanced Avg: {advancedProfit / assets.Count}");
        }

        [NotNull]
        private ITradeAccount Trade(
            IEnumerable<Candle> candles,
            [NotNull] Predicate<IIndexedOhlcv> buyRule,
            [NotNull] Predicate<IIndexedOhlcv> sellRule)
        {
            var tradeSession = new TradeSession(
                new TradeSessionConfig(
                    initialQuoteAmount: 1,
                    fee: 0.1m,
                    minQuoteAmount: 0.01m,
                    minProfitRatio: 0.2m),
                buyRule,
                sellRule);

            var result = tradeSession.Run(candles);

            return result;
        }

        [NotNull]
        private List<Entities.Candle> LoadCandles(string baseAsset, string quoteAsset, DateTime start, DateTime end,
            CandlesInterval interval)
        {
            const int maxRange = 500;
            var candles = new List<Entities.Candle>();

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
    }
}