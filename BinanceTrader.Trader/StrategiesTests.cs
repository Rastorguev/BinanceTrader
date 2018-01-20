using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Core.Entities;
using BinanceTrader.Tools;
using JetBrains.Annotations;

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
                //"POE",

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
                //"RCN",
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

            var initialAmountTotal = 0m;
            var tradeAmountTotal = 0m;
            var holdAmountTotal = 0m;

            foreach (var asset in assets)
            {
                var candles = LoadCandles(
                    asset,
                    "ETH",
                    new DateTime(2017, 10, 1, 0, 0, 0),
                    new DateTime(2017, 12, 1, 0, 0, 0),
                    CandlesInterval.Minutes1);

                var result = Trade(candles);

                if (!candles.Any())
                {
                    continue;
                }

                var firstPrice = candles.First().ClosePrice;
                var lastPrice = candles.Last().ClosePrice;

                var tradeQuoteAmount = result.CurrentBaseAmount * lastPrice + result.CurrentQuoteAmount;
                var holdQuoteAmount = result.InitialQuoteAmount / firstPrice * lastPrice + result.InitialBaseAmount;

                var tradeProfitPercents =
                    MathUtils.CalculateProfit(result.InitialQuoteAmount, tradeQuoteAmount).Round();
                var holdProfitPercents = MathUtils.CalculateProfit(result.InitialQuoteAmount, holdQuoteAmount).Round();
                var diffQuoteAmount = tradeQuoteAmount - holdQuoteAmount;
                var diffPercents = tradeProfitPercents - holdProfitPercents;

                initialAmountTotal += result.InitialQuoteAmount;
                tradeAmountTotal += tradeQuoteAmount;
                holdAmountTotal += holdQuoteAmount;

                Console.WriteLine(asset);
                Console.WriteLine();
                Console.WriteLine($"Trade Profit %:\t {tradeProfitPercents}");
                Console.WriteLine($"Hold Profit %:\t {holdProfitPercents}");
                Console.WriteLine($"Diff:\t\t {diffQuoteAmount.Round()}");
                Console.WriteLine($"Diff %:\t\t {diffPercents.Round()}");
                Console.WriteLine($"Trades Count:\t {result.TradesLog.Count}");
              


                if (result.TradesLog.Any())
                {
                    Console.WriteLine($"Last trade:\t {result.TradesLog.Last().Timestamp}");
                }

                Console.WriteLine();
            }

            var tradeProfit = MathUtils.CalculateProfit(initialAmountTotal, tradeAmountTotal).Round();
            var holdProfit = MathUtils.CalculateProfit(initialAmountTotal, holdAmountTotal).Round();

            Console.WriteLine();
            Console.WriteLine("----------------------");
            Console.WriteLine();
            Console.WriteLine($"Initial Total:\t\t {initialAmountTotal}");
            Console.WriteLine($"Trade Total:\t\t {tradeAmountTotal.Round()}");
            Console.WriteLine($"Hold Total:\t\t {holdAmountTotal.Round()}");
            Console.WriteLine($"Trade Profit Total %:\t {tradeProfit}");
            Console.WriteLine($"Hold Profit Total %:\t {holdProfit}");
        }

        [NotNull]
        private ITradeAccount Trade(
            List<Candle> candles)
        {
            var tradeSession = new TradeSession(
                new TradeSessionConfig(
                    initialQuoteAmount: 1,
                    initialPrice: 0,
                    fee: 0.1m,
                    minQuoteAmount: 0.01m,
                    minProfitRatio: 3));

            var result = tradeSession.Run(candles);

            return result;
        }

        [NotNull]
        private List<Candle> LoadCandles(
            string baseAsset,
            string quoteAsset,
            DateTime start,
            DateTime end,
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

                var rangeCandles = _api.GetCandles(baseAsset, quoteAsset, interval, start, rangeEnd)
                    .NotNull()
                    .Result.NotNull()
                    .ToList();

                candles.AddRange(rangeCandles);
                start = rangeEnd;
            }

            return candles.OrderBy(c => c.NotNull().OpenTime).ToList();
        }
    }
}