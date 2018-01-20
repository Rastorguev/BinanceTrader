using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Trady.Analysis;
using Trady.Analysis.Extension;
using Trady.Core;
using Trady.Core.Infrastructure;

namespace BinanceTrader
{
    public class StrategiesTests
    {
        [NotNull] private readonly BinanceApi _api;

        public StrategiesTests(
            [NotNull] BinanceApi api) => _api = api;

        public void DmiStrategyTest()
        {
            var candles = LoadCandles(
                "TRX",
                "ETH",
                new DateTime(2018, 1, 17, 0, 0, 0),
                new DateTime(2018, 1, 19, 20, 40, 0),
                CandlesInterval.Minutes1);

            var period = 14;

            var adx = candles.Adx(period).NotNull();
            var pdi = candles.Pdi(period).NotNull();
            var mdi = candles.Mdi(period).NotNull();
            //var dmi = candles.Dmi(period).NotNull();
            var buyCandles = new List<Candle>();

            var all = new List<decimal>();

            all.AddRange(adx.Where(x => x.Tick != null).Select(x => x.Tick.Value));
            all.AddRange(pdi.Where(x => x.Tick != null).Select(x => x.Tick.Value));
            all.AddRange(mdi.Where(x => x.Tick != null).Select(x => x.Tick.Value));
            var ordered = all.OrderBy(x => x);
            var lowerBound = ordered.First();
            var upperBound = ordered.Last();
            var mediumBound = upperBound + lowerBound / 2;

            for (var i = period; i < candles.Count; i++)
            {
                if (i == period)
                {
                    continue;
                }

                var prevIndex = i - 1;
                if (adx[i].Tick == null)
                {
                    continue;
                }

                if (candles[i].DateTime == new DateTime(2018, 1, 16, 13, 19, 0))
                {
                }

                var adxPrev = adx[prevIndex].Tick.Value;
                var adxCurrent = adx[i].Tick.Value;
                var pdiPrev = pdi[prevIndex].Tick.Value;
                var pdiCurrent = pdi[i].Tick.Value;
                var mdiPrev = mdi[prevIndex].Tick.Value;
                var mdiCurrent = mdi[i].Tick.Value;

                var isPdiTrough = IsTrough(pdi.Select(x => x.Tick), prevIndex);
                var isAdxPeak = IsPeak(adx.Select(x => x.Tick), prevIndex);

                if (adxPrev > 30 &&
                    pdiPrev <= 10 &&
                    isPdiTrough &&
                    //isAdxPeak &&
                    mdiCurrent < mdiPrev)
                {
                    buyCandles.Add(candles[i]);
                }
            }

            var buyDates = buyCandles.Select(x => x.DateTime).ToList();
        }

        private static bool IsTrough([NotNull] IEnumerable<decimal?> items, int index)
        {
            var itemsList = items.ToList();

            if (index < 1 || index > itemsList.Count - 2)
            {
                return false;
            }

            return
                itemsList[index] != null &&
                itemsList[index - 1] != null &&
                itemsList[index + 1] != null &&
                itemsList[index].Value < itemsList[index - 1].Value &&
                itemsList[index].Value < itemsList[index + 1].Value;
        }

        private static bool IsPeak([NotNull] IEnumerable<decimal?> items, int index)
        {
            var itemsList = items.ToList();

            if (index < 1 || index > itemsList.Count - 2)
            {
                return false;
            }

            return
                itemsList[index] != null &&
                itemsList[index - 1] != null &&
                itemsList[index + 1] != null &&
                itemsList[index].Value > itemsList[index - 1].Value &&
                itemsList[index].Value > itemsList[index + 1].Value;
        }

        public void CompareStrategies()
        {
            var assets = new List<string>
            {
                //"TRX"

                "TRX",
                "CND",
                "TNB",
                "POE",
                "FUN",
                "XVG",
                "MANA",
                "CDT",
                "LEND",
                "DNT"
            };

            var strategies =
                new List<(string Name, Predicate<IIndexedOhlcv> BuyRule, Predicate<IIndexedOhlcv> SellRule)>();

            const int shortPeriod = 7;
            const int longPeriod = 25;
            const int signalPeriod = 9;

            var tradeProfitTotal = 0m;
            var holdProfitTotal = 0m;

            var macdStrategy = ("MACD",
                Rule.Create(ic => ic.IsMacdBullishCross(shortPeriod, longPeriod, signalPeriod)),
                Rule.Create(ic => ic.IsMacdBearishCross(shortPeriod, longPeriod, signalPeriod)));

            var smaStrategy = ("SMA",
                Rule.Create(ic => ic.IsSmaBullishCross(shortPeriod, longPeriod)),
                Rule.Create(ic => ic.IsSmaBearishCross(shortPeriod, longPeriod)));

            //var advancedStrategy = ("Advanced",
            //    Rule.Create(ic => ic.IsStochRsiOversold(15)),
            //    Rule.Create(ic => ic.IsStochRsiOverbought(15)));

            strategies.Add(macdStrategy);
            //strategies.Add(smaStrategy);
            //strategies.Add(advancedStrategy);

            foreach (var asset in assets)
            {
                var candles = LoadCandles(
                    asset,
                    "ETH",

                    //DateTime.Now.AddDays(-6),
                    //DateTime.Now.AddDays(-5),
                    new DateTime(2017, 12, 1, 0, 0, 0),
                    new DateTime(2018, 01, 20, 0, 0, 0),
                    //DateTime.Now,
                    CandlesInterval.Minutes1);

                Console.WriteLine(asset);

                foreach (var strategy in strategies)
                {
                    //var initialPrice = candles.First().Close;
                    var result = Trade(candles, strategy.BuyRule, strategy.SellRule);

                    if (!candles.Any())
                    {
                        continue;
                    }

                    var firstPrice = candles.First().Close;
                    var lastPrice = candles.Last().Close;

                    var tradeQuoteAmount = result.CurrentBaseAmount * lastPrice + result.CurrentQuoteAmount;
                    var holdQuoteAmount = result.InitialQuoteAmount / firstPrice * lastPrice + result.InitialBaseAmount;

                    var tradeProfit = MathUtils.CalculateProfit(result.InitialQuoteAmount, tradeQuoteAmount).Round();
                    var holdProfit = MathUtils.CalculateProfit(result.InitialQuoteAmount, holdQuoteAmount).Round();
                    var diff = tradeQuoteAmount - holdQuoteAmount;
                    var diffPercents = tradeProfit - holdProfit;

                    tradeProfitTotal += tradeProfit;
                    holdProfitTotal += holdProfit;

                    Console.WriteLine($"Trade: {tradeProfit}");
                    Console.WriteLine($"Hold: {holdProfit}");
                    Console.WriteLine($"Diff: {diff}");
                    Console.WriteLine($"Diff %: {diffPercents}");
                    Console.WriteLine(result.TradesLog.Count);

                    if (result.TradesLog.Any())
                    {
                        Console.WriteLine(result.TradesLog.Last().Timestamp);
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine("----------------------");
            Console.WriteLine($"Trade Total: {tradeProfitTotal / assets.Count}");
            Console.WriteLine($"Hold Total: {holdProfitTotal / assets.Count}");
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
                    initialPrice: 0,
                    fee: 0.1m,
                    minQuoteAmount: 0.01m,
                    minProfitRatio: 0.2m),
                buyRule,
                sellRule);

            var result = tradeSession.Run(candles);

            return result;
        }

        [NotNull]
        private List<Candle> LoadCandles(string baseAsset, string quoteAsset, DateTime start, DateTime end,
            CandlesInterval interval)
        {
            const int maxRange = 500;
            var candles = new List<Core.Entities.Candle>();

            while (start < end)
            {
                var intervalMinutes = maxRange * interval.ToMinutes();
                var rangeEnd = (end - start).TotalMinutes > intervalMinutes
                    ? start.AddMinutes(intervalMinutes)
                    : end;

                var rangeCandles = _api.GetCandles(baseAsset, quoteAsset, interval, start, rangeEnd)
                    .NotNull()
                    .Result.NotNull()
                    .ToList();

                candles.AddRange(rangeCandles);
                start = rangeEnd;
            }

            return candles.ToTradyCandles().OrderBy(c => c.NotNull().DateTime).ToList();
        }
    }

    public static class Converters
    {
        [NotNull]
        public static List<Candle> ToTradyCandles(
            [NotNull] [ItemNotNull] this IEnumerable<Core.Entities.Candle> candles)
        {
            return candles.Select(c => c.ToTradyCandle()).ToList().NotNull();
        }

        public static Candle ToTradyCandle([NotNull] this Core.Entities.Candle candle) =>
            new Candle(
                candle.OpenTime,
                candle.OpenPrice,
                candle.HighPrice,
                candle.LowPrice,
                candle.ClosePrice,
                candle.Volume);
    }
}