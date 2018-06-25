using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class StrategiesTests
    {
        [NotNull] private readonly BinanceClient _binanceClient;
        [NotNull] private readonly CandlesProvider _candlesProvider;

        public StrategiesTests(
            [NotNull] BinanceClient client,
            [NotNull] CandlesProvider candlesProvider
        )
        {
            _binanceClient = client;
            _candlesProvider = candlesProvider;
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

            var configs = GenerateConfigs();
            var results = new Dictionary<TradeSessionConfig, TradeResult>();

            foreach (var config in configs)
            {
                Console.WriteLine($"{config.MinProfitRatio}/{config.MaxIdleHours}");
                Console.WriteLine();

                var initialAmountTotal = 0m;
                var tradeAmountTotal = 0m;
                var holdAmountTotal = 0m;

                foreach (var asset in assets)
                {
                    var candles = await _candlesProvider.GetCandles(
                        asset,
                        "ETH",
                        new DateTime(2018, 06, 19, 9, 0, 0),
                        new DateTime(2018, 06, 25, 9, 0, 0),
                        TimeInterval.Minutes_1);

                    var result = Trade(candles, config);

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

                    initialAmountTotal += result.InitialQuoteAmount;
                    tradeAmountTotal += tradeQuoteAmount;
                    holdAmountTotal += holdQuoteAmount;

                    //Console.WriteLine(asset);
                    //Console.WriteLine();
                    //Console.WriteLine($"Trade Amount:\t {tradeQuoteAmount.Round()}");
                    //Console.WriteLine($"Hold Amount:\t {holdQuoteAmount.Round()}");
                    //Console.WriteLine($"Trade Profit %:\t {tradeProfitPercents}");
                    //Console.WriteLine($"Hold Profit %:\t {holdProfitPercents}");
                    //Console.WriteLine($"Diff:\t\t {diffQuoteAmount.Round()}");
                    //Console.WriteLine($"Diff %:\t\t {MathUtils.Gain(holdQuoteAmount, tradeQuoteAmount).Round()}");
                    //Console.WriteLine($"Afficiency:\t {(tradeProfitPercents - holdProfitPercents).Round()}");
                    //Console.WriteLine($"Trades Count:\t {result.TradesLog.Count}");

                    //if (result.TradesLog.Any())
                    //{
                    //    Console.WriteLine($"Last trade:\t {result.TradesLog.Last().NotNull().Timestamp}");
                    //}

                    //Console.WriteLine();
                }

                var tradResult = new TradeResult(initialAmountTotal, tradeAmountTotal, holdAmountTotal);
                results[config] = tradResult;

                var tradeProfitTotalPercents = MathUtils.Gain(initialAmountTotal, tradeAmountTotal).Round();
                var holdProfitTotalPercents = MathUtils.Gain(initialAmountTotal, holdAmountTotal).Round();

                Console.WriteLine($"Initial Total:\t\t {tradResult.InitialAmount.Round()}");
                Console.WriteLine($"Trade Total:\t\t {tradResult.TradeAmount.Round()}");
                Console.WriteLine($"Hold Total:\t\t {tradResult.HoldAmount.Round()}");
                Console.WriteLine($"Trade Profit Total %:\t {tradResult.TradeProfit.Round()}");
                Console.WriteLine($"Hold Profit Total %:\t {tradResult.HoldProfit.Round()}");
                Console.WriteLine($"Diff %:\t\t {tradResult.Diff.Round()}");
                Console.WriteLine($"Afficiency:\t {tradResult.Afficiency.Round()}");
                Console.WriteLine("----------------------");
                Console.WriteLine();

                //var tradeProfitTotalPercents = MathUtils.Gain(initialAmountTotal, tradeAmountTotal).Round();
                //var holdProfitTotalPercents = MathUtils.Gain(initialAmountTotal, holdAmountTotal).Round();

                //Console.WriteLine();
                //Console.WriteLine("----------------------");
                //Console.WriteLine();
                //Console.WriteLine($"Initial Total:\t\t {initialAmountTotal}");
                //Console.WriteLine($"Trade Total:\t\t {tradeAmountTotal.Round()}");
                //Console.WriteLine($"Hold Total:\t\t {holdAmountTotal.Round()}");
                //Console.WriteLine($"Trade Profit Total %:\t {tradeProfitTotalPercents}");
                //Console.WriteLine($"Hold Profit Total %:\t {holdProfitTotalPercents}");
                //Console.WriteLine($"Diff %:\t\t {MathUtils.Gain(holdAmountTotal, tradeAmountTotal).Round()}");
                //Console.WriteLine($"Afficiency:\t {(tradeProfitTotalPercents - holdProfitTotalPercents).Round()}");
                //Console.WriteLine();
                //Console.WriteLine();
            }

            var ordered = results.OrderBy(r => r.Value.NotNull().Diff).ToList();
            var max = results.First();
            var min = results.Last();

        }

        [NotNull]
        private ITradeAccount Trade([NotNull] IReadOnlyList<Candlestick> candles, [NotNull] TradeSessionConfig config)
        {
            var tradeSession = new TradeSession(config);

            var result = tradeSession.Run(candles);

            return result;
        }

        private IReadOnlyList<TradeSessionConfig> GenerateConfigs()
        {
            TradeSessionConfig CreateConfig(decimal minProfit, decimal idle) =>
                new TradeSessionConfig(
                    initialQuoteAmount: 1m,
                    initialPrice: 0,
                    fee: 0.05m,
                    minQuoteAmount:
                    0.01m,
                    minProfitRatio: minProfit,
                    maxIdleHours: idle);

            var configs = new List<TradeSessionConfig>();

            const decimal profitStep = 0.5m;
            const decimal idleStep = 0.5m;

            var profit = 0.5m;
            while (profit <= 10)
            {
                var idle = 0.5m;
                while (idle <= 24m)
                {
                    configs.Add(CreateConfig(profit, idle));
                    idle += idleStep;
                }

                profit += profitStep;
            }

            return configs;
        }
    }

    public class TradeResult
    {
        public decimal InitialAmount { get; }
        public decimal TradeAmount { get; }
        public decimal HoldAmount { get; }
        public decimal TradeProfit { get; }
        public decimal HoldProfit { get; }
        public decimal Diff { get; }
        public decimal Afficiency { get; }

        public TradeResult(decimal initialAmount, decimal tradeAmount, decimal holdAmount)
        {
            InitialAmount = initialAmount;
            TradeAmount = tradeAmount;
            HoldAmount = holdAmount;
            TradeProfit = MathUtils.Gain(InitialAmount, TradeAmount);
            HoldProfit = MathUtils.Gain(InitialAmount, HoldAmount);
            Diff = MathUtils.Gain(HoldAmount, TradeAmount);
            Afficiency = TradeProfit - HoldProfit;
        }
    }
}