using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class StrategiesTests
    {
        [NotNull] private readonly CandlesProvider _candlesProvider;

        public StrategiesTests([NotNull] CandlesProvider candlesProvider) => _candlesProvider = candlesProvider;

        public List<KeyValuePair<TradeSessionConfig, TradeResult>> CompareStrategies()
        {
            var assets = AssetsProvider.Assets;

            var configs = GenerateConfigs();
            var results = new ConcurrentDictionary<TradeSessionConfig, TradeResult>();

            Parallel.ForEach(configs, config =>
            {
                var initialAmountTotal = 0m;
                var tradeAmountTotal = 0m;
                var holdAmountTotal = 0m;

                foreach (var asset in assets)
                {
                    var candles = _candlesProvider.GetCandles(
                        asset,
                        "ETH",
                        new DateTime(2018, 06, 19, 9, 0, 0),
                        new DateTime(2018, 06, 25, 9, 0, 0),
                        TimeInterval.Minutes_1).Result.NotNull();

                    if (!candles.Any())
                    {
                        continue;
                    }

                    var result = Trade(candles, config.NotNull());

                    var firstPrice = candles.First().NotNull().Close;
                    var lastPrice = candles.Last().NotNull().Close;

                    var tradeResult = new TradeResult(
                        config.InitialQuoteAmount,
                        result.CurrentBaseAmount * lastPrice + result.CurrentQuoteAmount,
                        result.InitialQuoteAmount / firstPrice * lastPrice + result.InitialBaseAmount);

                    initialAmountTotal += tradeResult.InitialAmount;
                    tradeAmountTotal += tradeResult.TradeAmount;
                    holdAmountTotal += tradeResult.HoldAmount;
                }

                var tradesResult = new TradeResult(initialAmountTotal, tradeAmountTotal, holdAmountTotal);
                results[config.NotNull()] = tradesResult;

                Console.WriteLine($"{config.ProfitRatio} / {config.MaxIdleHours}");
            });

           

            var ordered = results.OrderBy(r => r.Value.NotNull().TradeProfit).ToList();
            var max = ordered.Last();
            var current = ordered.FirstOrDefault(r =>
                r.Key.NotNull().ProfitRatio == 2 &&
                r.Key.NotNull().MaxIdleHours == 12);


            return ordered;
        }

        [NotNull]
        private static ITradeAccount Trade([NotNull] IReadOnlyList<Candlestick> candles,
            [NotNull] TradeSessionConfig config)
        {
            var tradeSession = new TradeSession(config);
            var result = tradeSession.Run(candles);

            return result;
        }

        [NotNull]
        [ItemNotNull]
        private static IReadOnlyList<TradeSessionConfig> GenerateConfigs()
        {
            TradeSessionConfig CreateConfig(decimal minProfit, decimal idle) =>
                new TradeSessionConfig(
                    initialQuoteAmount: 1m,
                    initialPrice: 0,
                    fee: 0.05m,
                    minQuoteAmount:
                    0.01m,
                    profitRatio: minProfit,
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