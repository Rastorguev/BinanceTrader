using System.Collections.Concurrent;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Market;
using BinanceTrader.Tools;

namespace BinanceTrader.Tests;

public class StrategiesTests
{
    private readonly CandlesProvider _candlesProvider;

    public StrategiesTests(CandlesProvider candlesProvider)
    {
        _candlesProvider = candlesProvider;
    }

    public static ConcurrentDictionary<TradeSessionConfig, TradeResult> CompareStrategies(
        Dictionary<string, IReadOnlyList<Candlestick>> candlesDict,
        IReadOnlyList<TradeSessionConfig> configs)
    {
        var results = new ConcurrentDictionary<TradeSessionConfig, TradeResult>();

        Parallel.ForEach(
            configs,
            //new ParallelOptions { MaxDegreeOfParallelism = 1 },
            config =>
            {
                Console.WriteLine($"Start: {config.NotNull().ProfitRatio} / {config.MaxIdlePeriod}");

                var tradeResults = new List<TradeResult>();

                foreach (var assetCandles in candlesDict)
                {
                    var candles = assetCandles.Value ?? new List<Candlestick>();

                    if (!candles.Any())
                    {
                        continue;
                    }

                    var account = Trade(candles, config.NotNull());
                    var firstPrice = candles[0].NotNull().Open;
                    var lastPrice = candles[^1].NotNull().Close;

                    var tradeResult = new TradeResult(
                        config.InitialQuoteAmount,
                        account.CurrentBaseAmount * lastPrice + account.CurrentQuoteAmount - account.TotalFee * config.FeeAssetToQuoteConversionRatio,
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

            var assetCandles = await _candlesProvider.LoadCandles(
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

    private static ITradeAccount Trade(IReadOnlyList<Candlestick> candles, TradeSessionConfig config)
    {
        var tradeSession = new TradeSession(config);
        var result = tradeSession.Run(candles);

        return result;
    }
}