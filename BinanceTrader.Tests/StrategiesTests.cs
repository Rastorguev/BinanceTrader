using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class StrategiesTests
    {
        [NotNull] private readonly BinanceClient _binanceClient;

        public StrategiesTests(
            [NotNull] BinanceClient api) => _binanceClient = api;

        public void CompareStrategies()
        {
            var assets = new List<string>
            {

                //"NCASH",
                "IOST",
                //"STORM",
                "TRX",
                "FUN",
                "POE",
                "TNB",
                "XVG",
                "CDT",
                "DNT",
                "LEND",
                "MANA",
                "SNGLS",
                "TNT",
                "FUEL",
                "YOYO",
                "CND",
                "RCN",
                "MTH",
                "CMT",
                "SNT",
                "RPX",
                "ENJ",
                "CHAT",
                "BTS",
                "VIB",
                "SNM",
                "OST",
                "QSP",
                "DLT",
                "BAT"
            };

            var initialAmountTotal = 0m;
            var tradeAmountTotal = 0m;
            var holdAmountTotal = 0m;

            foreach (var asset in assets)
            {
                var candles = LoadCandles(
                    asset,
                    "ETH",
                    new DateTime(2017, 09, 1, 0, 0, 0),
                    new DateTime(2018, 04, 22, 0, 0, 0),
                    TimeInterval.Minutes_1);

                var result = Trade(candles);

                if (!candles.Any())
                {
                    continue;
                }

                var firstPrice = candles.First().Close;
                var lastPrice = candles.Last().Close;

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
                Console.WriteLine($"Trade Amount:\t {tradeQuoteAmount.Round()}");
                Console.WriteLine($"Hold Amount:\t {holdQuoteAmount.Round()}");
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
            List<Candlestick> candles)
        {
            var tradeSession = new TradeSession(
                new TradeSessionConfig(
                    initialQuoteAmount: 0.04m,
                    initialPrice: 0,
                    fee: 0.1m,
                    minQuoteAmount: 0.01m,
                    minProfitRatio: 2m,
                    maxIdleHours: 12));

            var result = tradeSession.Run(candles);

            return result;
        }

        [NotNull]
        [ItemNotNull]
        private List<Candlestick> LoadCandles(
            string baseAsset,
            string quoteAsset,
            DateTime start,
            DateTime end,
            TimeInterval interval)
        {
            const int maxRange = 500;

            var tasks = new List<Task<IEnumerable<Candlestick>>>();


            while (start < end)
            {
                var intervalMinutes = maxRange * interval.ToMinutes();
                var rangeEnd = (end - start).TotalMinutes > intervalMinutes
                    ? start.AddMinutes(intervalMinutes)
                    : end;

                var symbol = $"{baseAsset}{quoteAsset}";


                tasks.Add(_binanceClient.GetCandleSticks(symbol, interval, start, rangeEnd));
                start = rangeEnd;
            }

            var candles = Task.WhenAll(tasks).NotNull().Result.NotNull().SelectMany(c => c).ToList();

            var orderedCandles = candles.OrderBy(c => c.NotNull().OpenTime).ToList();
            return orderedCandles;
        }
    }
}