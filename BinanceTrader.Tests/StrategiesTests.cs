﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Strategies;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class StrategiesTests
    {
        [NotNull] private readonly CandlesProvider _candlesProvider;

        public StrategiesTests(
            [NotNull] BinanceClient client)
        {
            _candlesProvider = new CandlesProvider(client);
        }

        public async Task CompareStrategies()
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

            var closesStrategy = new ClosesStrategy();

            foreach (var asset in assets)
            {
                var candles = await _candlesProvider.GetCandles(
                    asset,
                    "ETH",
                    new DateTime(2018, 01, 01, 0, 0, 0),
                    new DateTime(2018, 06, 01, 0, 0, 0),
                    TimeInterval.Minutes_1);

                var result = Trade(candles, closesStrategy);

                if (!candles.Any())
                {
                    continue;
                }

                var firstPrice = candles.First().NotNull().Close;
                var lastPrice = candles.Last().NotNull().Close;

                var tradeQuoteAmount = result.CurrentBaseAmount * lastPrice + result.CurrentQuoteAmount;
                var holdQuoteAmount = result.InitialQuoteAmount / firstPrice * lastPrice + result.InitialBaseAmount;

                var tradeProfitPercents =
                    MathUtils.Gain(result.InitialQuoteAmount, tradeQuoteAmount).Round();
                var holdProfitPercents = MathUtils.Gain(result.InitialQuoteAmount, holdQuoteAmount).Round();
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

            var tradeProfit = MathUtils.Gain(initialAmountTotal, tradeAmountTotal).Round();
            var holdProfit = MathUtils.Gain(initialAmountTotal, holdAmountTotal).Round();

            Console.WriteLine();
            Console.WriteLine("----------------------");
            Console.WriteLine();
            Console.WriteLine($"Initial Total:\t\t {initialAmountTotal}");
            Console.WriteLine($"Trade Total:\t\t {tradeAmountTotal.Round()}");
            Console.WriteLine($"Hold Total:\t\t {holdAmountTotal.Round()}");
            Console.WriteLine($"Trade Profit Total %:\t {tradeProfit}");
            Console.WriteLine($"Hold Profit Total %:\t {holdProfit}");
            Console.WriteLine($"Trading Efficiency %:\t {tradeProfit - holdProfit}");
        }

        [NotNull]
        private ITradeAccount Trade(
            [NotNull] IReadOnlyList<Candlestick> candles,
            [NotNull] ITradingStrategy strategy
        )
        {
            var tradeSession = new TradeSession(
                new TradeSessionConfig(
                    initialQuoteAmount: 1m,
                    initialPrice: 0,
                    fee: 0.1m,
                    minQuoteAmount: 0.01m,
                    minProfitRatio: 2m,
                    maxIdleHours: 12));

            var result = tradeSession.Run(candles, strategy);

            return result;
        }
    }
}