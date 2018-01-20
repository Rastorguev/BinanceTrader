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

        public void CompareStrategies()
        {
            var assets = new List<string>
            {
                "TRX",
                "CND",
                "TNB",
                "POE",
                "FUN",
                "XVG",
                "MANA",
                "CDT",
                "LEND",
                "DNT",
                "TNT",
                "ENJ",
                "FUEL",
                "YOYO",
                "SNGLS",
                "RCN",
                "CMT",
                "SNT",
                "MTH",
                "VIB",
                "BTS",
                "SNM",
                "XLM",
                "QSP",
                "GTO",
                "REQ",
                "BAT",
                "ADA",
                "OST",
                "LINK"
            };

            var tradeProfitTotal = 0m;
            var holdProfitTotal = 0m;

            foreach (var asset in assets)
            {
                var candles = LoadCandles(
                    asset,
                    "ETH",
                    new DateTime(2018, 1, 20, 0, 0, 0),
                    new DateTime(2018, 1, 20, 18, 0, 0),

                    CandlesInterval.Minutes1);

                Console.WriteLine(asset);

           
                    var result = Trade(candles);

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
            
                Console.WriteLine();
            }

            Console.WriteLine("----------------------");
            Console.WriteLine($"Trade Total: {tradeProfitTotal / assets.Count}");
            Console.WriteLine($"Hold Total: {holdProfitTotal / assets.Count}");
        }

        [NotNull]
        private ITradeAccount Trade(
            IEnumerable<Candle> candles)
        {
            var tradeSession = new TradeSession(
                new TradeSessionConfig(
                    initialQuoteAmount: 1,
                    initialPrice: 0,
                    fee: 0.1m,
                    minQuoteAmount: 0.01m,
                    minProfitRatio: 2));

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