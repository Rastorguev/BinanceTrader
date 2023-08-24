using System.Collections.Concurrent;
using BinanceApi.Models.Account;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Market;
using BinanceTrader.Core;
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
                Console.WriteLine($"Start: {config.ProfitRatio} / {config.MaxIdlePeriod}");

                var assetTradeResults = new List<AssetTradeResult>();

                foreach (var assetCandles in candlesDict)
                {
                    var candles = assetCandles.Value ?? new List<Candlestick>();

                    if (!candles.Any())
                    {
                        continue;
                    }

                    var account = Trade(candles, config);
                    var firstPrice = candles[0].Open;
                    var lastPrice = candles[^1].Close;

                    var tradeAmount = account.CurrentBaseAmount * lastPrice + account.CurrentQuoteAmount -
                                      account.TotalFee * config.FeeAssetToQuoteConversionRatio;

                    var assetTradeResult = new AssetTradeResult(
                        assetCandles.Key,
                        config.InitialQuoteAmount,
                        tradeAmount,
                        account.InitialQuoteAmount / firstPrice * lastPrice + account.InitialBaseAmount,
                        account.CompletedCount,
                        account.CanceledCount,
                        account.Trades,
                        config.FeeAssetToQuoteConversionRatio);

                    assetTradeResults.Add(assetTradeResult);
                }

                var tradeHistory = assetTradeResults.ToDictionary(x => x.BaseAsset, x => x.Trades);

                var tradesResult = new TradeResult(
                    assetTradeResults.Sum(r => r.InitialAmount),
                    assetTradeResults.Sum(r => r.TradeAmount),
                    assetTradeResults.Sum(r => r.HoldAmount),
                    assetTradeResults.Sum(r => r.CompletedCount),
                    assetTradeResults.Sum(r => r.CanceledCount),
                    TechAnalyzer.AnalyzeTradeHistory(tradeHistory, config.FeeAssetToQuoteConversionRatio));

                results[config] = tradesResult;

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

            candlesDict.Add(asset, assetCandles);

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