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

        public List<KeyValuePair<TradeSessionConfig, TradeResult>> CompareStrategies(
            [NotNull] IReadOnlyList<string> assets,
            [NotNull] IReadOnlyList<TradeSessionConfig> configs)
        {
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
    }
}