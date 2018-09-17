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

        [NotNull]
        public ConcurrentDictionary<TradeSessionConfig, TradeResult> CompareStrategies(
            [NotNull] Dictionary<string, IReadOnlyList<Candlestick>> candlesDict,
            [NotNull] IReadOnlyList<TradeSessionConfig> configs)
        {
            var results = new ConcurrentDictionary<TradeSessionConfig, TradeResult>();

            Parallel.ForEach(
                configs,
                //new ParallelOptions { MaxDegreeOfParallelism = 1 },
                config =>
                {
                    Console.WriteLine($"Start: {config.NotNull().ProfitRatio} / {config.MaxIdlePeriod}");

                    var tradeResults = new List<TradeResult>();

                    foreach (var value in candlesDict)
                    {
                        var candles = value.Value ?? new List<Candlestick>();

                        if (!candles.Any())
                        {
                            continue;
                        }

                        var account = Trade(candles, config.NotNull());
                        var firstPrice = candles.First().NotNull().Close;
                        var lastPrice = candles.Last().NotNull().Close;

                        var tradeResult = new TradeResult(
                            config.InitialQuoteAmount,
                            account.CurrentBaseAmount * lastPrice + account.CurrentQuoteAmount,
                            account.InitialQuoteAmount / firstPrice * lastPrice + account.InitialBaseAmount,
                            account.CompletedCount,
                            account.CanceledCount);

                        tradeResults.Add(tradeResult);
                    }

                    var tradesResult = new TradeResult(
                        tradeResults.Sum(r => r.NotNull().InitialAmount),
                        tradeResults.Sum(r => r.NotNull().TradeAmount),
                        tradeResults.Sum(r => r.NotNull().HoldAmount),
                        tradeResults.Sum(r => r.NotNull().CompletedCount),
                        tradeResults.Sum(r => r.NotNull().CanceledCount));

                    results[config.NotNull()] = tradesResult;

                    Console.WriteLine($"End: {config.ProfitRatio} / {config.MaxIdlePeriod}");
                });

            return results;
        }

        public async Task<Dictionary<string, IReadOnlyList<Candlestick>>> LoadCandles(
            IEnumerable<string> assets,
            string baseAsset,
            DateTime start,
            DateTime end,
            TimeInterval interval)
        {
            var candlesDict = new Dictionary<string, IReadOnlyList<Candlestick>>();

            foreach (var asset in assets)
            {
                Console.WriteLine($"{asset} load started");

                var assetCandles = await _candlesProvider.GetCandles(
                    asset,
                    baseAsset,
                    start,
                    end,
                    interval);

                candlesDict.Add(asset.NotNull(), assetCandles);

                Console.WriteLine($"{asset} load completed");
            }

            return candlesDict;
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